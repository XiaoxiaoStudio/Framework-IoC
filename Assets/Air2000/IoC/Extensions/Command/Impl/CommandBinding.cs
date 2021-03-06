﻿/*----------------------------------------------------------------
            // Copyright © 2014-2016 Air2000
            // 
            // FileName: CommandBinding.cs
			// Describle:
			// Created By:  Wells Hsu
			// Date&Time:  2016/9/18 16:48:17
            // Modify History:
            //
//----------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Air2000.IoC.Core;

namespace Air2000.IoC.Extensions.Command
{
    public class CommandBinding : Binding, ICommandBinding
    {
        public bool isOneOff { get; set; }

        public bool isSequence { get; set; }

        public bool isPooled { get; set; }

        public CommandBinding() : base()
        {
        }

        public CommandBinding(Binder.BindingResolver resolver) : base(resolver)
        {
        }

        public ICommandBinding Once()
        {
            isOneOff = true;
            return this;
        }

        public ICommandBinding InParallel()
        {
            isSequence = false;
            return this;
        }

        public ICommandBinding InSequence()
        {
            isSequence = true;
            return this;
        }

        public ICommandBinding Pooled()
        {
            isPooled = true;
            resolver(this);
            return this;
        }

        //Everything below this point is simply facade on Binding to ensure fluent interface


        new public ICommandBinding Bind<T>()
        {
            return base.Bind<T>() as ICommandBinding;
        }

        new public ICommandBinding Bind(object key)
        {
            return base.Bind(key) as ICommandBinding;
        }

        new public ICommandBinding To<T>()
        {
            return base.To<T>() as ICommandBinding;
        }

        new public ICommandBinding To(object o)
        {
            return base.To(o) as ICommandBinding;
        }

        new public ICommandBinding ToName<T>()
        {
            return base.ToName<T>() as ICommandBinding;
        }

        new public ICommandBinding ToName(object o)
        {
            return base.ToName(o) as ICommandBinding;
        }

        new public ICommandBinding Named<T>()
        {
            return base.Named<T>() as ICommandBinding;
        }

        new public ICommandBinding Named(object o)
        {
            return base.Named(o) as ICommandBinding;
        }
    }
}
