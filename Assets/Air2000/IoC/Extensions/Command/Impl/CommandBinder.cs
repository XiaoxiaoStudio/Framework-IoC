﻿/*----------------------------------------------------------------
            // Copyright © 2014-2016 Air2000
            // 
            // FileName: CommandBinder.cs
			// Describle:
			// Created By:  Wells Hsu
			// Date&Time:  2016/9/18 16:48:09
            // Modify History:
            //
//----------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Air2000.IoC.Core;
using Air2000.IoC.Extensions.ObjPool;
using Air2000.IoC.Extensions.Injector;
using Air2000.IoC.Extensions.Dispatcher;

namespace Air2000.IoC.Extensions.Command
{
    public class CommandBinder : Binder, ICommandBinder, IPooledCommandBinder, ITriggerable
    {
        [Inject]
        public IInjectionBinder injectionBinder { get; set; }

        protected Dictionary<Type, Pool> pools = new Dictionary<Type, Pool>();

        /// Tracker for parallel commands in progress
        protected HashSet<ICommand> activeCommands = new HashSet<ICommand>();

        /// Tracker for sequences in progress
        protected Dictionary<ICommand, ICommandBinding> activeSequences = new Dictionary<ICommand, ICommandBinding>();

        public CommandBinder()
        {
            usePooling = true;
        }

        public override IBinding GetRawBinding()
        {
            return new CommandBinding(resolver);
        }

        public virtual void ReactTo(object trigger)
        {
            ReactTo(trigger, null);
        }

        public virtual void ReactTo(object trigger, object data)
        {
            if (data is IPoolable)
            {
                (data as IPoolable).Retain();
            }
            ICommandBinding binding = GetBinding(trigger) as ICommandBinding;
            if (binding != null)
            {
                if (binding.isSequence)
                {
                    next(binding, data, 0);
                }
                else
                {
                    object[] values = binding.value as object[];
                    int aa = values.Length + 1;
                    for (int a = 0; a < aa; a++)
                    {
                        next(binding, data, a);
                    }
                }
            }
        }

        protected void next(ICommandBinding binding, object data, int depth)
        {
            object[] values = binding.value as object[];
            if (depth < values.Length)
            {
                Type cmd = values[depth] as Type;
                ICommand command = invokeCommand(cmd, binding, data, depth);
                ReleaseCommand(command);
            }
            else
            {
                disposeOfSequencedData(data);
                if (binding.isOneOff)
                {
                    Unbind(binding);
                }
            }
        }

        //EventCommandBinder (and perhaps other sub-classes) use this method to dispose of the data in sequenced commands
        protected virtual void disposeOfSequencedData(object data)
        {
            //No-op. Override if necessary.
        }

        protected virtual ICommand invokeCommand(Type cmd, ICommandBinding binding, object data, int depth)
        {
            ICommand command = createCommand(cmd, data);
            command.sequenceId = depth;
            trackCommand(command, binding);
            executeCommand(command);
            return command;
        }

        protected virtual ICommand createCommand(object cmd, object data)
        {
            ICommand command = getCommand(cmd as Type);

            if (command == null)
            {
                string msg = "A Command ";
                if (data != null)
                {
                    msg += "tied to data " + data.ToString();
                }
                msg += " could not be instantiated.\nThis might be caused by a null pointer during instantiation or failing to override Execute (generally you shouldn't have constructor code in Commands).";
                throw new CommandException(msg, CommandExceptionType.BAD_CONSTRUCTOR);
            }

            command.data = data;
            return command;
        }

        protected ICommand getCommand(Type type)
        {
            if (usePooling && pools.ContainsKey(type))
            {
                Pool pool = pools[type];
                ICommand command = pool.GetInstance() as ICommand;
                if (command.IsClean)
                {
                    injectionBinder.injector.Inject(command);
                    command.IsClean = false;
                }
                return command;
            }
            else
            {
                injectionBinder.Bind<ICommand>().To(type);
                ICommand command = injectionBinder.GetInstance<ICommand>();
                injectionBinder.Unbind<ICommand>();
                return command;
            }
        }

        protected void trackCommand(ICommand command, ICommandBinding binding)
        {
            if (binding.isSequence)
            {
                activeSequences.Add(command, binding);
            }
            else
            {
                activeCommands.Add(command);
            }
        }

        protected void executeCommand(ICommand command)
        {
            if (command == null)
            {
                return;
            }
            command.Execute();
        }

        public virtual void Stop(object key)
        {
            if (key is ICommand && activeSequences.ContainsKey(key as ICommand))
            {
                removeSequence(key as ICommand);
            }
            else
            {
                ICommandBinding binding = GetBinding(key) as ICommandBinding;
                if (binding != null)
                {
                    if (activeSequences.ContainsValue(binding))
                    {
                        foreach (KeyValuePair<ICommand, ICommandBinding> sequence in activeSequences)
                        {
                            if (sequence.Value == binding)
                            {
                                ICommand command = sequence.Key;
                                removeSequence(command);
                            }
                        }
                    }
                }
            }
        }

        public virtual void ReleaseCommand(ICommand command)
        {
            if (command.retain == false)
            {
                Type t = command.GetType();
                if (usePooling && pools.ContainsKey(t))
                {
                    pools[t].ReturnInstance(command);
                }
                if (activeCommands.Contains(command))
                {
                    activeCommands.Remove(command);
                }
                else if (activeSequences.ContainsKey(command))
                {
                    ICommandBinding binding = activeSequences[command];
                    object data = command.data;
                    activeSequences.Remove(command);
                    next(binding, data, command.sequenceId + 1);
                }
            }
        }

        public bool usePooling { get; set; }

        public Pool<T> GetPool<T>()
        {
            Type t = typeof(T);
            if (pools.ContainsKey(t as Type))
                return pools[t] as Pool<T>;
            return null;
        }

        private void removeSequence(ICommand command)
        {
            if (activeSequences.ContainsKey(command))
            {
                command.Cancel();
                activeSequences.Remove(command);
            }
        }

        public bool Trigger<T>(object data)
        {
            return Trigger(typeof(T), data);
        }

        public bool Trigger(object key, object data)
        {
            ReactTo(key, data);
            return true;
        }

        new public virtual ICommandBinding Bind<T>()
        {
            return base.Bind<T>() as ICommandBinding;
        }

        new public virtual ICommandBinding Bind(object value)
        {
            return base.Bind(value) as ICommandBinding;
        }

        protected override void resolver(IBinding binding)
        {
            base.resolver(binding);
            if (usePooling && (binding as ICommandBinding).isPooled)
            {
                if (binding.value != null)
                {
                    object[] values = binding.value as object[];
                    foreach (Type value in values)
                    {
                        if (pools.ContainsKey(value) == false)
                        {
                            var myPool = makePoolFromType(value);
                            pools[value] = myPool;
                        }
                    }
                }
            }
        }

        protected virtual Pool makePoolFromType(Type type)
        {
            Type poolType = typeof(Pool<>).MakeGenericType(type);

            injectionBinder.Bind(type).To(type);
            injectionBinder.Bind<Pool>().To(poolType).ToName(CommandKeys.COMMAND_POOL);
            Pool pool = injectionBinder.GetInstance<Pool>(CommandKeys.COMMAND_POOL) as Pool;
            injectionBinder.Unbind<Pool>(CommandKeys.COMMAND_POOL);
            return pool;
        }

        new public virtual ICommandBinding GetBinding<T>()
        {
            return base.GetBinding<T>() as ICommandBinding;
        }
    }
}
