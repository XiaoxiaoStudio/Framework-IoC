﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Ioc.framework.api;
using Assets.Ioc.framework.impl;
using Assets.Ioc.extensions.injector.api;

namespace Assets.Ioc.extensions.injector.impl
{
    public class InjectionBinding : Binding, IInjectionBinding
    {
        private InjectionBindingType _type = InjectionBindingType.DEFAULT;
        private bool _toInject = true;
        private bool _isCrossContext = false;

        public InjectionBinding(Binder.BindingResolver resolver)
        {
            this.resolver = resolver;
            keyConstraint = BindingConstraintType.MANY;
            valueConstraint = BindingConstraintType.ONE;
        }
        public InjectionBindingType type
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value;
            }
        }

        public bool toInject
        {
            get
            {
                return _toInject;
            }
        }

        public IInjectionBinding ToInject(bool value)
        {
            _toInject = value;
            return this;
        }

        public bool isCrossContext
        {
            get
            {
                return _isCrossContext;
            }
        }

        public IInjectionBinding ToSingleton()
        {
            // If already a value,this mapping is redundant.
            if (type == InjectionBindingType.VALUE)
                return this;
            type = InjectionBindingType.SINGLETON;
            if (resolver != null)
            {
                resolver(this);
            }
            return this;
        }

        public IInjectionBinding ToValue(object o)
        {
            type = InjectionBindingType.VALUE;
            SetValue(o);
            return this;
        }

        public IInjectionBinding SetValue(object o)
        {
            Type objType = o.GetType();

            object[] keys = key as object[];
            int aa = keys.Length;
            //Check that value is legal for the provided keys
            for (int a = 0; a < aa; a++)
            {
                object aKey = keys[a];
                Type keyType = (aKey is Type) ? aKey as Type : aKey.GetType();
                if (keyType.IsAssignableFrom(objType) == false && (HasGenericAssignableFrom(keyType, objType) == false))
                {
                    throw new InjectionException("Injection cannot bind a value that does not extend or implement the binding type.", InjectionExceptionType.ILLEGAL_BINDING_VALUE);
                }
            }
            To(o);
            return this;
        }

        protected bool HasGenericAssignableFrom(Type keyType, Type objType)
        {
            if (keyType.IsGenericType == false)
                return false;

            return true;
        }


        protected bool IsGenericTypeAssignable(Type givenType, Type genericType)
        {
            var interfaceTypes = givenType.GetInterfaces();

            foreach (var it in interfaceTypes)
            {
                if (it.IsGenericType && it.GetGenericTypeDefinition() == genericType)
                    return true;
            }

            if (givenType.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
                return true;

            Type baseType = givenType.BaseType;
            if (baseType == null) return false;

            return IsGenericTypeAssignable(baseType, genericType);
        }

        public IInjectionBinding CrossContext()
        {
            _isCrossContext = true;
            if (resolver != null)
            {
                resolver(this);
            }
            return this;
        }

        new public IInjectionBinding Bind<T>()
        {
            return base.Bind<T>() as IInjectionBinding;
        }

        new public IInjectionBinding Bind(object key)
        {
            return base.Bind(key) as IInjectionBinding;
        }

        new public IInjectionBinding To<T>()
        {
            return base.To<T>() as IInjectionBinding;
        }

        new public IInjectionBinding To(object o)
        {
            return base.To(o) as IInjectionBinding;
        }

        new public IInjectionBinding ToName<T>()
        {
            return base.ToName<T>() as IInjectionBinding;
        }

        new public IInjectionBinding ToName(object o)
        {
            return base.ToName(o) as IInjectionBinding;
        }

        new public IInjectionBinding Named<T>()
        {
            return base.Named<T>() as IInjectionBinding;
        }

        new public IInjectionBinding Named(object o)
        {
            return base.Named(o) as IInjectionBinding;
        }
    }
}
