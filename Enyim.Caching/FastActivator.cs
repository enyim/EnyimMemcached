using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;

namespace Enyim.Reflection
{
	/// <summary>
	/// <para>Implements a very fast object factory for dynamic object creation. Dynamically generates a factory class which will use the new() constructor of the requested type.</para>
	/// <para>Much faster than using Activator at the price of the first invocation being significantly slower than subsequent calls.</para>
	/// </summary>
	public static class FastActivator
	{
		private static log4net.ILog log = log4net.LogManager.GetLogger(typeof(FastActivator));

		private static SimpleFactory simpleFactory = new SimpleFactory();
		private static MultiArgFactory multiArgFactory = new MultiArgFactory();

		/// <summary>
		/// Creates an instance of the specified type using a generated factory to avoid using Reflection.
		/// </summary>
		/// <param name="type">The type to be created</param>
		/// <returns>The newly created instance.</returns>
		public static object CreateInstance(Type type)
		{
			return simpleFactory.CreateInstance(type);
		}

		/// <summary>
		/// Creates an instance of the specified type using a generated factory to avoid using Reflection. It will use the constructor which has the specified type of arguments.
		/// </summary>
		/// <param name="type">The type to be created.</param>
		/// <param name="ctor">The types of the contructor parameters.</param>
		/// <param name="args">The arguments used to initialize the new instance.</param>
		/// <returns>The newly created instance.</returns>
		public static object CreateInstance(Type type, Type[] ctor, object[] args)
		{
			return multiArgFactory.CreateInstance(type, ctor, args);
		}

		/// <summary>
		/// Creates an instance of the specified type using a generated factory to avoid using Reflection.
		/// </summary>
		/// <typeparam name="T">The type to be created.</typeparam>
		/// <returns>The newly created instance.</returns>
		public static T CreateInstance<T>()
		{
			return (T)simpleFactory.CreateInstance(typeof(T));
		}

		/// <summary>
		/// Creates an instance of the specified type using a generated factory to avoid using Reflection. It will use the constructor which has the specified type of arguments.
		/// </summary>
		/// <typeparam name="T">The type to be created.</typeparam>
		/// <param name="ctor">The types of the contructor parameters.</param>
		/// <param name="args">The arguments used to initialize the new instance.</param>
		/// <returns>The newly created instance.</returns>
		public static T CreateInstance<T>(Type[] ctor, object[] args)
		{
			return (T)multiArgFactory.CreateInstance(typeof(T), ctor, args);
		}

		#region [ Helper                       ]
		static class Helper
		{

			public static Type IFastObjectFacory = typeof(IFastObjectFacory);
			public static Type IFastMultiArgObjectFacory = typeof(IFastMultiArgObjectFacory);
			public static Type Object = typeof(object);
			public static Type[] ObjectArrayParam = new Type[] { typeof(object[]) };
			private static int counter = 1;
			public const MethodAttributes DefaultMethodAttrs = MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.SpecialName | MethodAttributes.Final;

			public static readonly ModuleBuilder Module = CreateBuilder();


			public static string MakeSafeName(Type type)
			{
				StringBuilder retval = new StringBuilder();

				retval.Append("generated.F_");
				retval.Append(type.FullName);
				retval.Replace('+', '_');
				retval.Replace('.', '_');

				retval.Append('_').Append(counter++);

				return retval.ToString();
			}

			private static ModuleBuilder CreateBuilder()
			{
				AppDomain domain = Thread.GetDomain();
				AssemblyName name = new AssemblyName();

				name.Name = Path.GetRandomFileName();
				name.Version = new Version(1, 0, 0, 0);
				name.Flags = AssemblyNameFlags.EnableJITcompileOptimizer;

				AssemblyBuilder assemblyBuilder = domain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
				assemblyBuilder.DefineVersionInfoResource();

				return assemblyBuilder.DefineDynamicModule(Path.GetRandomFileName());
			}
		}
		#endregion
		#region [ SimpleFactory                ]
		class SimpleFactory
		{
			private Dictionary<Type, IFastObjectFacory> factoryCache = new Dictionary<Type, IFastObjectFacory>();

			public object CreateInstance(Type type)
			{
				if (type.IsNotPublic)
				{
					log.WarnFormat("Type {0} is not public. Falling back to Activator.", type);

					return Activator.CreateInstance(type);
				}

				return this.GetFactory(type).CreateInstance();
			}

			private IFastObjectFacory GetFactory(Type type)
			{
				IFastObjectFacory retval;

				if (!this.factoryCache.TryGetValue(type, out retval))
				{
					lock (this.factoryCache)
					{
						if (!this.factoryCache.TryGetValue(type, out retval))
						{
							retval = (IFastObjectFacory)Activator.CreateInstance(EmitFactory(type));

							Thread.MemoryBarrier();

							this.factoryCache.Add(type, retval);
						}
					}
				}

				return retval;
			}

			private static Type EmitFactory(Type type)
			{
				ModuleBuilder module = Helper.Module;
				// get the .ctor() of the type
				ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
				if (constructor == null)
					throw new InvalidOperationException(String.Format("The type {0} must have a .ctor().", type.FullName));

				TypeBuilder typeBuilder = module.DefineType(Helper.MakeSafeName(type), TypeAttributes.Public | TypeAttributes.Class);
				typeBuilder.AddInterfaceImplementation(Helper.IFastObjectFacory);
				typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);

				// object IFastObjectFacory.Create()
				MethodBuilder methodBuilder = typeBuilder.DefineMethod(Helper.IFastObjectFacory.FullName + ".CreateInstance", Helper.DefaultMethodAttrs, Helper.Object, Type.EmptyTypes);

				ILGenerator ilgen = methodBuilder.GetILGenerator();

				// implement the interface member
				typeBuilder.DefineMethodOverride(methodBuilder, Helper.IFastObjectFacory.GetMethod("CreateInstance", Type.EmptyTypes));
				ilgen.Emit(OpCodes.Newobj, constructor);
				ilgen.Emit(OpCodes.Ret);

				return typeBuilder.CreateType();
			}
		}
		#endregion
		#region [ MultiArgFactory              ]
		class MultiArgFactory
		{
			private Dictionary<int, IFastMultiArgObjectFacory> factoryCache = new Dictionary<int, IFastMultiArgObjectFacory>();
			static OpCode[] ldcI4List = new OpCode[]
			{ 
				OpCodes.Ldc_I4_0, OpCodes.Ldc_I4_1, OpCodes.Ldc_I4_2, OpCodes.Ldc_I4_3,
				OpCodes.Ldc_I4_4, OpCodes.Ldc_I4_5, OpCodes.Ldc_I4_6, OpCodes.Ldc_I4_7,
				OpCodes.Ldc_I4_8
			};

			public object CreateInstance(Type type, Type[] ctorArgs, object[] ctorValues)
			{
				if (type.IsNotPublic)
				{
					log.WarnFormat("Type {0} is not public. Falling back to Activator.", type);

					return Activator.CreateInstance(type, ctorValues);
				}

				return this.GetFactory(type, ctorArgs).CreateInstance(ctorValues);
			}

			private IFastMultiArgObjectFacory GetFactory(Type type, Type[] ctorArgs)
			{
				IFastMultiArgObjectFacory retval;

				HashCodeCombiner hcc = new HashCodeCombiner();
				for (int i = 0; i < ctorArgs.Length; i++)
				{
					hcc.Add(ctorArgs[i].GetHashCode());
				}

				if (!this.factoryCache.TryGetValue(hcc.CurrentHash, out retval))
				{
					lock (this.factoryCache)
					{
						if (!this.factoryCache.TryGetValue(hcc.CurrentHash, out retval))
						{
							retval = (IFastMultiArgObjectFacory)Activator.CreateInstance(EmitMultiArgFactory(type, ctorArgs));

							Thread.MemoryBarrier();

							this.factoryCache.Add(hcc.CurrentHash, retval);
						}
					}
				}

				return retval;
			}

			private static Type EmitMultiArgFactory(Type type, Type[] ctorArgs)
			{
				ModuleBuilder module = Helper.Module;

				// get the .ctor() of the type
				ConstructorInfo constructor = type.GetConstructor(ctorArgs);
				if (constructor == null)
					throw new InvalidOperationException(String.Format(".ctor with {1} args not found for {0}", type.FullName, ctorArgs.Length));

				TypeBuilder typeBuilder = module.DefineType(Helper.MakeSafeName(type), TypeAttributes.Public | TypeAttributes.Class);
				typeBuilder.AddInterfaceImplementation(Helper.IFastMultiArgObjectFacory);
				typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);

				// object IFastObjectFacory.Create()
				MethodBuilder methodBuilder = typeBuilder.DefineMethod(Helper.IFastMultiArgObjectFacory.FullName + ".CreateInstance", Helper.DefaultMethodAttrs, Helper.Object, Helper.ObjectArrayParam);

				ILGenerator ilgen = methodBuilder.GetILGenerator();

				// implement the interface member
				typeBuilder.DefineMethodOverride(methodBuilder, Helper.IFastMultiArgObjectFacory.GetMethod("CreateInstance", Helper.ObjectArrayParam));

				/*
					.method private hidebysig newslot virtual final instance object ActivatorTest.Program.IFactory.Create(object[] args) cil managed

					.override ActivatorTest.Program/IFactory::Create
					.maxstack 8
					L_0000: ldarg.1 
					L_0001: ldc.i4.0 
					L_0002: ldelem.ref 
					L_0003: unbox.any int32
					L_0008: ldarg.1 
					L_0009: ldc.i4.1 
					L_000a: ldelem.ref 
					L_000b: unbox.any uint8
					L_0010: ldarg.1 
					L_0011: ldc.i4.2 
					L_0012: ldelem.ref 
					L_0013: unbox.any int64
					L_0018: ldarg.1 
					L_0019: ldc.i4.3 
					L_001a: ldelem.ref 
					L_001b: castclass string[]
					L_0020: newobj instance void ActivatorTest.Program/Impl1::.ctor(int32, uint8, int64, string[])
					L_0025: ret 
				*/
				for (int i = 0; i < ctorArgs.Length; i++)
				{
					Type arg = ctorArgs[i];

					ilgen.Emit(OpCodes.Ldarg_1);

					if (i < 9)
						ilgen.Emit(ldcI4List[i]);
					else
						ilgen.Emit(OpCodes.Ldc_I4, i);

					ilgen.Emit(OpCodes.Ldelem_Ref);
					ilgen.Emit(arg.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, arg);
				}

				ilgen.Emit(OpCodes.Newobj, constructor);
				ilgen.Emit(OpCodes.Ret);

				return typeBuilder.CreateType();
			}
		}
		#endregion
	}
}

#region [ License information          ]
/* ************************************************************
 *
 * Copyright (c) Attila Kiskó, enyim.com
 *
 * This source code is subject to terms and conditions of 
 * Microsoft Permissive License (Ms-PL).
 * 
 * A copy of the license can be found in the License.html
 * file at the root of this distribution. If you can not 
 * locate the License, please send an email to a@enyim.com
 * 
 * By using this source code in any fashion, you are 
 * agreeing to be bound by the terms of the Microsoft 
 * Permissive License.
 *
 * You must not remove this notice, or any other, from this
 * software.
 *
 * ************************************************************/
#endregion