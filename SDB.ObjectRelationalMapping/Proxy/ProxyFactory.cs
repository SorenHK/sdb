using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Reflection.Emit;
using SDB.DataServices;

namespace SDB.ObjectRelationalMapping.Proxy
{
    class ProxyFactory
    {
        #region Properties

        public AssemblyBuilder AssemblyBuilder
        {
            get { return _assemblyBuilder; }
            set { _assemblyBuilder = value; }
        }
        private AssemblyBuilder _assemblyBuilder;

        public ModuleBuilder ModuleBuilder
        {
            get { return _moduleBuilder; }
            set { _moduleBuilder = value; }
        }
        private ModuleBuilder _moduleBuilder;

        private TypeBuilder _typeBuilder;

        #endregion

        public ProxyFactory()
        {
            var mAsmName = new AssemblyName();
            mAsmName.Name = "SDB.Dynamic.dll";
            var currentDomain = AppDomain.CurrentDomain;

            _assemblyBuilder = currentDomain.DefineDynamicAssembly(mAsmName, AssemblyBuilderAccess.RunAndSave);
            _moduleBuilder = _assemblyBuilder.DefineDynamicModule(mAsmName.Name, mAsmName.Name, false);
        }

        public Type CreateType(String typeName, Type parent)
        {
            _typeBuilder = _moduleBuilder.DefineType(typeName,
                                                                  TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass |
                                                                  TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout,
                                                                  parent, new[] { typeof(IProxy) });
            _typeBuilder.AddInterfaceImplementation(typeof(IProxy));

            ImplementProxyInterface(parent);
            var propHandlerDic = OverrideProperties(parent);
            GenerateConstructor(propHandlerDic);

            return _typeBuilder.CreateType();
        }

        private void ImplementProxyInterface(Type parentType)
        {
            MethodBuilder idGetMethod, idSetMethod;
            DefineAutoProperty("SDBId", typeof(int), out idGetMethod, out idSetMethod);

            OverrideEquals(parentType, idGetMethod);

            ImplementINotifyPropertyChanged();
        }

        private Dictionary<PropertyInfo, FieldBuilder> OverrideProperties(Type parent)
        {
            var result = new Dictionary<PropertyInfo, FieldBuilder>();

            foreach (var pinfo in parent.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var getter = pinfo.GetGetMethod();
                var setter = pinfo.GetSetMethod();

                if ((getter == null || !getter.IsVirtual) && (setter == null || !setter.IsVirtual))
                    continue;

                var handlerType = GetPropertyHandlerType(pinfo.PropertyType);
                var handlerField = _typeBuilder.DefineField(GetHandlerName(pinfo), handlerType, FieldAttributes.Private);

                var pb = _typeBuilder.DefineProperty(pinfo.Name, PropertyAttributes.None, pinfo.PropertyType, Type.EmptyTypes);
                const MethodAttributes getSetAttr = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual;

                if (getter != null && getter.IsVirtual)
                {
                    var getMethod = _typeBuilder.DefineMethod(getter.Name, getSetAttr, pinfo.PropertyType, Type.EmptyTypes);
                    var gen = getMethod.GetILGenerator();
                    gen.Emit(OpCodes.Ldarg_0);
                    gen.Emit(OpCodes.Ldfld, handlerField);
                    gen.Emit(OpCodes.Callvirt, handlerType.GetMethod("get_Value"));
                    gen.Emit(OpCodes.Ret);
                    pb.SetGetMethod(getMethod);
                }

                if (setter != null && setter.IsVirtual)
                {
                    var setMethod = _typeBuilder.DefineMethod(setter.Name, getSetAttr, null, new[] { pinfo.PropertyType });
                    var gen = setMethod.GetILGenerator();
                    gen.Emit(OpCodes.Ldarg_0);
                    gen.Emit(OpCodes.Ldfld, handlerField);
                    gen.Emit(OpCodes.Ldarg_1);
                    gen.Emit(OpCodes.Callvirt, handlerType.GetMethod("set_Value"));
                    gen.Emit(OpCodes.Nop);
                    gen.Emit(OpCodes.Ret);
                    pb.SetSetMethod(setMethod);
                }

                result.Add(pinfo, handlerField);
            }

            return result;
        }

        private Type GetPropertyHandlerType(Type propertyType)
        {
            if (propertyType.IsValueType)
                return typeof(StructPropertyLoadHandler<>).MakeGenericType(new[] { propertyType });

            if (propertyType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(propertyType))
            {
                var arguments = propertyType.GetGenericArguments();
                if (arguments.Length > 1)
                    throw new InvalidOperationException("The property-type " + propertyType.Name + " has too many generic type arguments (more than one is currently not supported).");

                var itemType = arguments.Length == 0 ? typeof(object) : arguments[0];

                return typeof(CollectionPropertyLoadHandler<,>).MakeGenericType(new[] { propertyType, itemType });
            }

            return typeof(ClassPropertyLoadHandler<>).MakeGenericType(new[] { propertyType });
        }

        private ConstructorInfo GetPropertyHandlerTypeConstructor(Type propertyType)
        {
            var handlerType = GetPropertyHandlerType(propertyType);
            return handlerType.GetConstructor(new[] { typeof(IProxy), typeof(string), typeof(ObjectMapper) });
        }

        private void GenerateConstructor(Dictionary<PropertyInfo, FieldBuilder> propHandlerDic)
        {
            const MethodAttributes ctorAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;

            var args = new[] { typeof(ObjectMapper) };
            var ctor = _typeBuilder.DefineConstructor(ctorAttributes, CallingConventions.Standard, args);
            var gen = ctor.GetILGenerator();

            var typeResolver = typeof(Type).GetMethod("GetTypeFromHandle");

            foreach (var kv in propHandlerDic)
            {
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Ldarg_0); // add 'this' as parameter to PropertyLoadHandler ctor
                gen.Emit(OpCodes.Ldstr, kv.Key.Name);
                gen.Emit(OpCodes.Call, typeResolver);
                gen.Emit(OpCodes.Ldarg_1);
                gen.Emit(OpCodes.Newobj, GetPropertyHandlerTypeConstructor(kv.Key.PropertyType));
                gen.Emit(OpCodes.Stfld, kv.Value);
            }

            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Ret);
        }

        private void OverrideEquals(Type parentType, MethodBuilder idGetMethod)
        {
            const MethodAttributes equalsAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual;

            var getMethod = _typeBuilder.DefineMethod("Equals", equalsAttributes, typeof(bool), new[] { typeof(object) });
            var gen = getMethod.GetILGenerator();

            var baseLabel = gen.DefineLabel();

            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Isinst, typeof(IProxy));
            gen.Emit(OpCodes.Brfalse_S, baseLabel);

            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Isinst, typeof(IProxy));
            gen.Emit(OpCodes.Callvirt, typeof(IProxy).GetMethod("get_SDBId"));
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Call, idGetMethod);
            gen.Emit(OpCodes.Ceq);
            gen.Emit(OpCodes.Ret);

            gen.MarkLabel(baseLabel);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Call, parentType.GetMethod("Equals"));
            gen.Emit(OpCodes.Ret);
        }

        private void DefineAutoProperty(string name, Type type)
        {
            MethodBuilder getMethod, setMethod;
            DefineAutoProperty(name, type, out getMethod, out setMethod);
        }

        private void DefineAutoProperty(string name, Type type, out MethodBuilder getMethod, out MethodBuilder setMethod)
        {
            const MethodAttributes autoPropAttr = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.SpecialName | MethodAttributes.Virtual | MethodAttributes.Final;

            var field = _typeBuilder.DefineField("_" + name, typeof(int), FieldAttributes.Private);

            var pb = _typeBuilder.DefineProperty(name, PropertyAttributes.None, type, Type.EmptyTypes);

            getMethod = _typeBuilder.DefineMethod("get_" + name, autoPropAttr, type, Type.EmptyTypes);
            var gen = getMethod.GetILGenerator();
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldfld, field);
            gen.Emit(OpCodes.Ret);
            pb.SetGetMethod(getMethod);

            setMethod = _typeBuilder.DefineMethod("set_" + name, autoPropAttr, null, new[] { type });
            gen = setMethod.GetILGenerator();
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Stfld, field);
            gen.Emit(OpCodes.Ret);
            pb.SetSetMethod(setMethod);
        }

        private static string GetHandlerName(PropertyInfo property)
        {
            return "_" + property.Name + "Handler";
        }

        private MethodBuilder ImplementINotifyPropertyChanged()
        {
            const MethodAttributes eventDelegateAttributes =
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName |
                MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot;
            const MethodAttributes onPropertyChangedMethodAttributes =
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot |
                MethodAttributes.Virtual | MethodAttributes.Final;

            var DelegateCombine = typeof(Delegate).GetMethod("Combine", new Type[] { typeof(Delegate), typeof(Delegate) });
            var DelegateRemove = typeof(Delegate).GetMethod("Remove", new Type[] { typeof(Delegate), typeof(Delegate) });
            var InvokeDelegate = typeof(PropertyChangedEventHandler).GetMethod("Invoke");
            var eventBack = _typeBuilder.DefineField("PropertyChanged", typeof(PropertyChangingEventHandler), FieldAttributes.Private);
            var CreateEventArgs = typeof(PropertyChangingEventArgs).GetConstructor(new Type[] { typeof(String) });


            var AddPropertyChanged = _typeBuilder.DefineMethod(
                "add_PropertyChanged", eventDelegateAttributes,
                typeof(void), new Type[] { typeof(PropertyChangedEventHandler) });
            var gen = AddPropertyChanged.GetILGenerator();
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldfld, eventBack);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Call, DelegateCombine);
            gen.Emit(OpCodes.Castclass, typeof(PropertyChangedEventHandler));
            gen.Emit(OpCodes.Stfld, eventBack);
            gen.Emit(OpCodes.Ret);

            var RemovePropertyChanged = _typeBuilder.DefineMethod(
                "remove_PropertyChanged", eventDelegateAttributes,
                typeof(void), new Type[] { typeof(PropertyChangedEventHandler) });
            gen = RemovePropertyChanged.GetILGenerator();
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldfld, eventBack);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Call, DelegateRemove);
            gen.Emit(OpCodes.Castclass, typeof(PropertyChangedEventHandler));
            gen.Emit(OpCodes.Stfld, eventBack);
            gen.Emit(OpCodes.Ret);

            var RaisePropertyChangedMethod = _typeBuilder.DefineMethod("OnPropertyChanged", onPropertyChangedMethodAttributes, typeof(void), new Type[] { typeof(String) });
            gen = RaisePropertyChangedMethod.GetILGenerator();
            var lblDelegateOk = gen.DefineLabel();
            gen.DeclareLocal(typeof(PropertyChangedEventHandler));
            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldfld, eventBack);
            gen.Emit(OpCodes.Stloc_0);
            gen.Emit(OpCodes.Ldloc_0);
            gen.Emit(OpCodes.Ldnull);
            gen.Emit(OpCodes.Ceq);
            gen.Emit(OpCodes.Brtrue, lblDelegateOk);
            gen.Emit(OpCodes.Ldloc_0);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Newobj, CreateEventArgs);
            gen.Emit(OpCodes.Callvirt, InvokeDelegate);
            gen.MarkLabel(lblDelegateOk);
            gen.Emit(OpCodes.Ret);

            var pcevent = _typeBuilder.DefineEvent("PropertyChanged", EventAttributes.None, typeof(PropertyChangedEventHandler));
            pcevent.SetRaiseMethod(RaisePropertyChangedMethod);
            pcevent.SetAddOnMethod(AddPropertyChanged);
            pcevent.SetRemoveOnMethod(RemovePropertyChanged);

            return RaisePropertyChangedMethod;
        }

        internal void Save(string name)
        {
            if (_assemblyBuilder != null)
                _assemblyBuilder.Save(name);
        }
    }
}
