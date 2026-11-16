#if BEPINEX
using System;
using System.Reflection;
using ifapp.Game.Data;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppSystem.Collections.Generic;

namespace InFalsusChartExt.CustomSong.Install
{
    internal static class BepInExValueTypeListBridge
    {
        private const string AddMethodField =
            "NativeMethodInfoPtr_Add_Public_Virtual_Final_New_Void_T_0";
        private const string SetItemMethodField =
            "NativeMethodInfoPtr_set_Item_Public_Virtual_Final_New_" +
            "set_Void_Int32_T_0";

        internal static void Add<T>(List<T> list, T value)
        {
            Invoke(
                list,
                AddMethods.Pointer,
                value,
                -1);
        }

        internal static void SetItem<T>(
            List<T> list,
            int index,
            T value)
        {
            Invoke(
                list,
                SetItemMethods.Pointer,
                value,
                index);
        }

        private static unsafe void Invoke(
            Il2CppObjectBase list,
            IntPtr methodInfo,
            object value,
            int index)
        {
            if (list is null || list.Pointer == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    "Value-type list is unavailable.");
            }
            if (value is not Il2CppObjectBase nativeValue ||
                nativeValue.Pointer == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    "Value-type list item is unavailable.");
            }

            IntPtr valuePointer = nativeValue.Pointer;
            IntPtr valueClass =
                IL2CPP.il2cpp_object_get_class(valuePointer);
            if (valueClass != IntPtr.Zero &&
                IL2CPP.il2cpp_class_is_valuetype(valueClass))
            {
                valuePointer =
                    IL2CPP.il2cpp_object_unbox(valuePointer);
            }

            IntPtr* arguments = stackalloc IntPtr[2];
            int argumentCount;
            if (index >= 0)
            {
                int indexValue = index;
                arguments[0] = (IntPtr)(&indexValue);
                arguments[1] = valuePointer;
                argumentCount = 2;
            }
            else
            {
                arguments[0] = valuePointer;
                argumentCount = 1;
            }

            IntPtr exception = IntPtr.Zero;
            IL2CPP.il2cpp_runtime_invoke(
                methodInfo,
                list.Pointer,
                (void**)arguments,
                ref exception);
            GC.KeepAlive(nativeValue);
            if (exception != IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    $"Native value-type list method failed " +
                    $"({argumentCount} arguments).");
            }
        }

        private static class AddMethods
        {
            internal static readonly IntPtr Pointer =
                Resolve(AddMethodField);
        }

        private static class SetItemMethods
        {
            internal static readonly IntPtr Pointer =
                Resolve(SetItemMethodField);
        }

        private static IntPtr Resolve(string fieldName)
        {
            FieldInfo field =
                typeof(List<DynamicStringMapping.TextMappingValues>)
                    .GetField(
                        fieldName,
                        BindingFlags.Static |
                        BindingFlags.NonPublic);
            if (field is null ||
                field.GetValue(null) is not IntPtr pointer ||
                pointer == IntPtr.Zero)
            {
                throw new MissingFieldException(
                    typeof(List<
                        DynamicStringMapping.TextMappingValues>)
                        .FullName,
                    fieldName);
            }
            return pointer;
        }
    }
}
#endif
