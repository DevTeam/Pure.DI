// ReSharper disable InconsistentNaming
// ReSharper disable EmptyConstructor
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming
#pragma warning disable CS9113 // Parameter is unread.

namespace Pure.DI.IntegrationTests;

using Pure.DI;

// Total bindings: 341

public partial class HugeComposition
{
	private void Setup() => DI.Setup()
		.Bind<IRoot>().As(Lifetime.PerResolve).To<Root>().Root<IRoot>("Root");

	private void Setup_0() => DI.Setup()
		.Bind<IDependency_0_0_0_0>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_0_0_0_0>(out var val20);
			return val20;
		}).Root<IDependency_0_0_0_0>();

	private void Setup_1() => DI.Setup()
		.Bind<IDependency_0_0_0_1>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_0_0_0_1>(out var val21);
			return val21;
		}).Root<IDependency_0_0_0_1>();

	private void Setup_2() => DI.Setup()
		.Bind<IDependency_0_0_0_2>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_0_0_0_2>(out var val22);
			return val22;
		}).Root<IDependency_0_0_0_2>();

	private void Setup_3() => DI.Setup()
		.Bind<IDependency_0_0_0_3>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_0_0_0_3>(out var val23);
			return val23;
		}).Root<IDependency_0_0_0_3>();

	private void Setup_4() => DI.Setup()
		.Bind<IDependency_0_0_0>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_0_0_0>(out var val10);
			return val10;
		}).Root<IDependency_0_0_0>();

	private void Setup_5() => DI.Setup()
		.Bind<IDependency_0_0_1_0>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_0_0_1_0>(out var val20);
			return val20;
		}).Root<IDependency_0_0_1_0>();

	private void Setup_6() => DI.Setup()
		.Bind<IDependency_0_0_1_1>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_0_0_1_1>(out var val21);
			return val21;
		}).Root<IDependency_0_0_1_1>();

	private void Setup_7() => DI.Setup()
		.Bind<IDependency_0_0_1_2>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_0_0_1_2>(out var val22);
			return val22;
		}).Root<IDependency_0_0_1_2>();

	private void Setup_8() => DI.Setup()
		.Bind<IDependency_0_0_1_3>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_0_0_1_3>(out var val23);
			return val23;
		}).Root<IDependency_0_0_1_3>();

	private void Setup_9() => DI.Setup()
		.Bind<IDependency_0_0_1>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_0_0_1>(out var val11);
			return val11;
		}).Root<IDependency_0_0_1>();

	private void Setup_10() => DI.Setup()
		.Bind<IDependency_0_0_2_0>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_0_0_2_0>(out var val20);
			return val20;
		}).Root<IDependency_0_0_2_0>();

	private void Setup_11() => DI.Setup()
		.Bind<IDependency_0_0_2_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_0_0_2_1>(out var val21);
			return val21;
		}).Root<IDependency_0_0_2_1>();

	private void Setup_12() => DI.Setup()
		.Bind<IDependency_0_0_2_2>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_0_0_2_2>(out var val22);
			return val22;
		}).Root<IDependency_0_0_2_2>();

	private void Setup_13() => DI.Setup()
		.Bind<IDependency_0_0_2_3>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_0_0_2_3>(out var val23);
			return val23;
		}).Root<IDependency_0_0_2_3>();

	private void Setup_14() => DI.Setup()
		.Bind<IDependency_0_0_2>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_0_0_2>(out var val12);
			return val12;
		}).Root<IDependency_0_0_2>();

	private void Setup_15() => DI.Setup()
		.Bind<IDependency_0_0_3_0>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_0_0_3_0>(out var val20);
			return val20;
		}).Root<IDependency_0_0_3_0>();

	private void Setup_16() => DI.Setup()
		.Bind<IDependency_0_0_3_1>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_0_0_3_1>(out var val21);
			return val21;
		}).Root<IDependency_0_0_3_1>();

	private void Setup_17() => DI.Setup()
		.Bind<IDependency_0_0_3_2>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_0_0_3_2>(out var val22);
			return val22;
		}).Root<IDependency_0_0_3_2>();

	private void Setup_18() => DI.Setup()
		.Bind<IDependency_0_0_3_3>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_0_0_3_3>(out var val23);
			return val23;
		}).Root<IDependency_0_0_3_3>();

	private void Setup_19() => DI.Setup()
		.Bind<IDependency_0_0_3>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_0_0_3>(out var val13);
			return val13;
		}).Root<IDependency_0_0_3>();

	private void Setup_20() => DI.Setup()
		.Bind<IDependency_0_0>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_0_0>(out var val00);
			return val00;
		}).Root<IDependency_0_0>();

	private void Setup_21() => DI.Setup()
		.Bind<IDependency_0_1_0_0>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_0_1_0_0>(out var val20);
			return val20;
		}).Root<IDependency_0_1_0_0>();

	private void Setup_22() => DI.Setup()
		.Bind<IDependency_0_1_0_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_0_1_0_1>(out var val21);
			return val21;
		}).Root<IDependency_0_1_0_1>();

	private void Setup_23() => DI.Setup()
		.Bind<IDependency_0_1_0_2>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_0_1_0_2>(out var val22);
			return val22;
		}).Root<IDependency_0_1_0_2>();

	private void Setup_24() => DI.Setup()
		.Bind<IDependency_0_1_0_3>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_0_1_0_3>(out var val23);
			return val23;
		}).Root<IDependency_0_1_0_3>();

	private void Setup_25() => DI.Setup()
		.Bind<IDependency_0_1_0>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_0_1_0>(out var val10);
			return val10;
		}).Root<IDependency_0_1_0>();

	private void Setup_26() => DI.Setup()
		.Bind<IDependency_0_1_1_0>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_0_1_1_0>(out var val20);
			return val20;
		}).Root<IDependency_0_1_1_0>();

	private void Setup_27() => DI.Setup()
		.Bind<IDependency_0_1_1_1>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_0_1_1_1>(out var val21);
			return val21;
		}).Root<IDependency_0_1_1_1>();

	private void Setup_28() => DI.Setup()
		.Bind<IDependency_0_1_1_2>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_0_1_1_2>(out var val22);
			return val22;
		}).Root<IDependency_0_1_1_2>();

	private void Setup_29() => DI.Setup()
		.Bind<IDependency_0_1_1_3>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_0_1_1_3>(out var val23);
			return val23;
		}).Root<IDependency_0_1_1_3>();

	private void Setup_30() => DI.Setup()
		.Bind<IDependency_0_1_1>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_0_1_1>(out var val11);
			return val11;
		}).Root<IDependency_0_1_1>();

	private void Setup_31() => DI.Setup()
		.Bind<IDependency_0_1_2_0>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_0_1_2_0>(out var val20);
			return val20;
		}).Root<IDependency_0_1_2_0>();

	private void Setup_32() => DI.Setup()
		.Bind<IDependency_0_1_2_1>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_0_1_2_1>(out var val21);
			return val21;
		}).Root<IDependency_0_1_2_1>();

	private void Setup_33() => DI.Setup()
		.Bind<IDependency_0_1_2_2>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_0_1_2_2>(out var val22);
			return val22;
		}).Root<IDependency_0_1_2_2>();

	private void Setup_34() => DI.Setup()
		.Bind<IDependency_0_1_2_3>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_0_1_2_3>(out var val23);
			return val23;
		}).Root<IDependency_0_1_2_3>();

	private void Setup_35() => DI.Setup()
		.Bind<IDependency_0_1_2>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_0_1_2>(out var val12);
			return val12;
		}).Root<IDependency_0_1_2>();

	private void Setup_36() => DI.Setup()
		.Bind<IDependency_0_1_3_0>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_0_1_3_0>(out var val20);
			return val20;
		}).Root<IDependency_0_1_3_0>();

	private void Setup_37() => DI.Setup()
		.Bind<IDependency_0_1_3_1>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_0_1_3_1>(out var val21);
			return val21;
		}).Root<IDependency_0_1_3_1>();

	private void Setup_38() => DI.Setup()
		.Bind<IDependency_0_1_3_2>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_0_1_3_2>(out var val22);
			return val22;
		}).Root<IDependency_0_1_3_2>();

	private void Setup_39() => DI.Setup()
		.Bind<IDependency_0_1_3_3>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_0_1_3_3>(out var val23);
			return val23;
		}).Root<IDependency_0_1_3_3>();

	private void Setup_40() => DI.Setup()
		.Bind<IDependency_0_1_3>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_0_1_3>(out var val13);
			return val13;
		}).Root<IDependency_0_1_3>();

	private void Setup_41() => DI.Setup()
		.Bind<IDependency_0_1>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_0_1>(out var val01);
			return val01;
		}).Root<IDependency_0_1>();

	private void Setup_42() => DI.Setup()
		.Bind<IDependency_0_2_0_0>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_0_2_0_0>(out var val20);
			return val20;
		}).Root<IDependency_0_2_0_0>();

	private void Setup_43() => DI.Setup()
		.Bind<IDependency_0_2_0_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_0_2_0_1>(out var val21);
			return val21;
		}).Root<IDependency_0_2_0_1>();

	private void Setup_44() => DI.Setup()
		.Bind<IDependency_0_2_0_2>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_0_2_0_2>(out var val22);
			return val22;
		}).Root<IDependency_0_2_0_2>();

	private void Setup_45() => DI.Setup()
		.Bind<IDependency_0_2_0_3>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_0_2_0_3>(out var val23);
			return val23;
		}).Root<IDependency_0_2_0_3>();

	private void Setup_46() => DI.Setup()
		.Bind<IDependency_0_2_0>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_0_2_0>(out var val10);
			return val10;
		}).Root<IDependency_0_2_0>();

	private void Setup_47() => DI.Setup()
		.Bind<IDependency_0_2_1_0>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_0_2_1_0>(out var val20);
			return val20;
		}).Root<IDependency_0_2_1_0>();

	private void Setup_48() => DI.Setup()
		.Bind<IDependency_0_2_1_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_0_2_1_1>(out var val21);
			return val21;
		}).Root<IDependency_0_2_1_1>();

	private void Setup_49() => DI.Setup()
		.Bind<IDependency_0_2_1_2>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_0_2_1_2>(out var val22);
			return val22;
		}).Root<IDependency_0_2_1_2>();

	private void Setup_50() => DI.Setup()
		.Bind<IDependency_0_2_1_3>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_0_2_1_3>(out var val23);
			return val23;
		}).Root<IDependency_0_2_1_3>();

	private void Setup_51() => DI.Setup()
		.Bind<IDependency_0_2_1>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_0_2_1>(out var val11);
			return val11;
		}).Root<IDependency_0_2_1>();

	private void Setup_52() => DI.Setup()
		.Bind<IDependency_0_2_2_0>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_0_2_2_0>(out var val20);
			return val20;
		}).Root<IDependency_0_2_2_0>();

	private void Setup_53() => DI.Setup()
		.Bind<IDependency_0_2_2_1>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_0_2_2_1>(out var val21);
			return val21;
		}).Root<IDependency_0_2_2_1>();

	private void Setup_54() => DI.Setup()
		.Bind<IDependency_0_2_2_2>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_0_2_2_2>(out var val22);
			return val22;
		}).Root<IDependency_0_2_2_2>();

	private void Setup_55() => DI.Setup()
		.Bind<IDependency_0_2_2_3>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_0_2_2_3>(out var val23);
			return val23;
		}).Root<IDependency_0_2_2_3>();

	private void Setup_56() => DI.Setup()
		.Bind<IDependency_0_2_2>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_0_2_2>(out var val12);
			return val12;
		}).Root<IDependency_0_2_2>();

	private void Setup_57() => DI.Setup()
		.Bind<IDependency_0_2_3_0>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_0_2_3_0>(out var val20);
			return val20;
		}).Root<IDependency_0_2_3_0>();

	private void Setup_58() => DI.Setup()
		.Bind<IDependency_0_2_3_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_0_2_3_1>(out var val21);
			return val21;
		}).Root<IDependency_0_2_3_1>();

	private void Setup_59() => DI.Setup()
		.Bind<IDependency_0_2_3_2>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_0_2_3_2>(out var val22);
			return val22;
		}).Root<IDependency_0_2_3_2>();

	private void Setup_60() => DI.Setup()
		.Bind<IDependency_0_2_3_3>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_0_2_3_3>(out var val23);
			return val23;
		}).Root<IDependency_0_2_3_3>();

	private void Setup_61() => DI.Setup()
		.Bind<IDependency_0_2_3>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_0_2_3>(out var val13);
			return val13;
		}).Root<IDependency_0_2_3>();

	private void Setup_62() => DI.Setup()
		.Bind<IDependency_0_2>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_0_2>(out var val02);
			return val02;
		}).Root<IDependency_0_2>();

	private void Setup_63() => DI.Setup()
		.Bind<IDependency_0_3_0_0>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_0_3_0_0>(out var val20);
			return val20;
		}).Root<IDependency_0_3_0_0>();

	private void Setup_64() => DI.Setup()
		.Bind<IDependency_0_3_0_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_0_3_0_1>(out var val21);
			return val21;
		}).Root<IDependency_0_3_0_1>();

	private void Setup_65() => DI.Setup()
		.Bind<IDependency_0_3_0_2>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_0_3_0_2>(out var val22);
			return val22;
		}).Root<IDependency_0_3_0_2>();

	private void Setup_66() => DI.Setup()
		.Bind<IDependency_0_3_0_3>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_0_3_0_3>(out var val23);
			return val23;
		}).Root<IDependency_0_3_0_3>();

	private void Setup_67() => DI.Setup()
		.Bind<IDependency_0_3_0>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_0_3_0>(out var val10);
			return val10;
		}).Root<IDependency_0_3_0>();

	private void Setup_68() => DI.Setup()
		.Bind<IDependency_0_3_1_0>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_0_3_1_0>(out var val20);
			return val20;
		}).Root<IDependency_0_3_1_0>();

	private void Setup_69() => DI.Setup()
		.Bind<IDependency_0_3_1_1>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_0_3_1_1>(out var val21);
			return val21;
		}).Root<IDependency_0_3_1_1>();

	private void Setup_70() => DI.Setup()
		.Bind<IDependency_0_3_1_2>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_0_3_1_2>(out var val22);
			return val22;
		}).Root<IDependency_0_3_1_2>();

	private void Setup_71() => DI.Setup()
		.Bind<IDependency_0_3_1_3>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_0_3_1_3>(out var val23);
			return val23;
		}).Root<IDependency_0_3_1_3>();

	private void Setup_72() => DI.Setup()
		.Bind<IDependency_0_3_1>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_0_3_1>(out var val11);
			return val11;
		}).Root<IDependency_0_3_1>();

	private void Setup_73() => DI.Setup()
		.Bind<IDependency_0_3_2_0>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_0_3_2_0>(out var val20);
			return val20;
		}).Root<IDependency_0_3_2_0>();

	private void Setup_74() => DI.Setup()
		.Bind<IDependency_0_3_2_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_0_3_2_1>(out var val21);
			return val21;
		}).Root<IDependency_0_3_2_1>();

	private void Setup_75() => DI.Setup()
		.Bind<IDependency_0_3_2_2>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_0_3_2_2>(out var val22);
			return val22;
		}).Root<IDependency_0_3_2_2>();

	private void Setup_76() => DI.Setup()
		.Bind<IDependency_0_3_2_3>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_0_3_2_3>(out var val23);
			return val23;
		}).Root<IDependency_0_3_2_3>();

	private void Setup_77() => DI.Setup()
		.Bind<IDependency_0_3_2>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_0_3_2>(out var val12);
			return val12;
		}).Root<IDependency_0_3_2>();

	private void Setup_78() => DI.Setup()
		.Bind<IDependency_0_3_3_0>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_0_3_3_0>(out var val20);
			return val20;
		}).Root<IDependency_0_3_3_0>();

	private void Setup_79() => DI.Setup()
		.Bind<IDependency_0_3_3_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_0_3_3_1>(out var val21);
			return val21;
		}).Root<IDependency_0_3_3_1>();

	private void Setup_80() => DI.Setup()
		.Bind<IDependency_0_3_3_2>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_0_3_3_2>(out var val22);
			return val22;
		}).Root<IDependency_0_3_3_2>();

	private void Setup_81() => DI.Setup()
		.Bind<IDependency_0_3_3_3>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_0_3_3_3>(out var val23);
			return val23;
		}).Root<IDependency_0_3_3_3>();

	private void Setup_82() => DI.Setup()
		.Bind<IDependency_0_3_3>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_0_3_3>(out var val13);
			return val13;
		}).Root<IDependency_0_3_3>();

	private void Setup_83() => DI.Setup()
		.Bind<IDependency_0_3>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_0_3>(out var val03);
			return val03;
		}).Root<IDependency_0_3>();

	private void Setup_84() => DI.Setup()
		.Bind<IDependency_1_0_0_0>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_1_0_0_0>(out var val20);
			return val20;
		}).Root<IDependency_1_0_0_0>();

	private void Setup_85() => DI.Setup()
		.Bind<IDependency_1_0_0_1>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_1_0_0_1>(out var val21);
			return val21;
		}).Root<IDependency_1_0_0_1>();

	private void Setup_86() => DI.Setup()
		.Bind<IDependency_1_0_0_2>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_1_0_0_2>(out var val22);
			return val22;
		}).Root<IDependency_1_0_0_2>();

	private void Setup_87() => DI.Setup()
		.Bind<IDependency_1_0_0_3>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_1_0_0_3>(out var val23);
			return val23;
		}).Root<IDependency_1_0_0_3>();

	private void Setup_88() => DI.Setup()
		.Bind<IDependency_1_0_0>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_1_0_0>(out var val10);
			return val10;
		}).Root<IDependency_1_0_0>();

	private void Setup_89() => DI.Setup()
		.Bind<IDependency_1_0_1_0>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_1_0_1_0>(out var val20);
			return val20;
		}).Root<IDependency_1_0_1_0>();

	private void Setup_90() => DI.Setup()
		.Bind<IDependency_1_0_1_1>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_1_0_1_1>(out var val21);
			return val21;
		}).Root<IDependency_1_0_1_1>();

	private void Setup_91() => DI.Setup()
		.Bind<IDependency_1_0_1_2>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_1_0_1_2>(out var val22);
			return val22;
		}).Root<IDependency_1_0_1_2>();

	private void Setup_92() => DI.Setup()
		.Bind<IDependency_1_0_1_3>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_1_0_1_3>(out var val23);
			return val23;
		}).Root<IDependency_1_0_1_3>();

	private void Setup_93() => DI.Setup()
		.Bind<IDependency_1_0_1>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_1_0_1>(out var val11);
			return val11;
		}).Root<IDependency_1_0_1>();

	private void Setup_94() => DI.Setup()
		.Bind<IDependency_1_0_2_0>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_1_0_2_0>(out var val20);
			return val20;
		}).Root<IDependency_1_0_2_0>();

	private void Setup_95() => DI.Setup()
		.Bind<IDependency_1_0_2_1>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_1_0_2_1>(out var val21);
			return val21;
		}).Root<IDependency_1_0_2_1>();

	private void Setup_96() => DI.Setup()
		.Bind<IDependency_1_0_2_2>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_1_0_2_2>(out var val22);
			return val22;
		}).Root<IDependency_1_0_2_2>();

	private void Setup_97() => DI.Setup()
		.Bind<IDependency_1_0_2_3>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_1_0_2_3>(out var val23);
			return val23;
		}).Root<IDependency_1_0_2_3>();

	private void Setup_98() => DI.Setup()
		.Bind<IDependency_1_0_2>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_1_0_2>(out var val12);
			return val12;
		}).Root<IDependency_1_0_2>();

	private void Setup_99() => DI.Setup()
		.Bind<IDependency_1_0_3_0>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_1_0_3_0>(out var val20);
			return val20;
		}).Root<IDependency_1_0_3_0>();

	private void Setup_100() => DI.Setup()
		.Bind<IDependency_1_0_3_1>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_1_0_3_1>(out var val21);
			return val21;
		}).Root<IDependency_1_0_3_1>();

	private void Setup_101() => DI.Setup()
		.Bind<IDependency_1_0_3_2>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_1_0_3_2>(out var val22);
			return val22;
		}).Root<IDependency_1_0_3_2>();

	private void Setup_102() => DI.Setup()
		.Bind<IDependency_1_0_3_3>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_1_0_3_3>(out var val23);
			return val23;
		}).Root<IDependency_1_0_3_3>();

	private void Setup_103() => DI.Setup()
		.Bind<IDependency_1_0_3>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_1_0_3>(out var val13);
			return val13;
		}).Root<IDependency_1_0_3>();

	private void Setup_104() => DI.Setup()
		.Bind<IDependency_1_0>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_1_0>(out var val00);
			return val00;
		}).Root<IDependency_1_0>();

	private void Setup_105() => DI.Setup()
		.Bind<IDependency_1_1_0_0>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_1_1_0_0>(out var val20);
			return val20;
		}).Root<IDependency_1_1_0_0>();

	private void Setup_106() => DI.Setup()
		.Bind<IDependency_1_1_0_1>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_1_1_0_1>(out var val21);
			return val21;
		}).Root<IDependency_1_1_0_1>();

	private void Setup_107() => DI.Setup()
		.Bind<IDependency_1_1_0_2>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_1_1_0_2>(out var val22);
			return val22;
		}).Root<IDependency_1_1_0_2>();

	private void Setup_108() => DI.Setup()
		.Bind<IDependency_1_1_0_3>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_1_1_0_3>(out var val23);
			return val23;
		}).Root<IDependency_1_1_0_3>();

	private void Setup_109() => DI.Setup()
		.Bind<IDependency_1_1_0>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_1_1_0>(out var val10);
			return val10;
		}).Root<IDependency_1_1_0>();

	private void Setup_110() => DI.Setup()
		.Bind<IDependency_1_1_1_0>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_1_1_1_0>(out var val20);
			return val20;
		}).Root<IDependency_1_1_1_0>();

	private void Setup_111() => DI.Setup()
		.Bind<IDependency_1_1_1_1>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_1_1_1_1>(out var val21);
			return val21;
		}).Root<IDependency_1_1_1_1>();

	private void Setup_112() => DI.Setup()
		.Bind<IDependency_1_1_1_2>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_1_1_1_2>(out var val22);
			return val22;
		}).Root<IDependency_1_1_1_2>();

	private void Setup_113() => DI.Setup()
		.Bind<IDependency_1_1_1_3>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_1_1_1_3>(out var val23);
			return val23;
		}).Root<IDependency_1_1_1_3>();

	private void Setup_114() => DI.Setup()
		.Bind<IDependency_1_1_1>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_1_1_1>(out var val11);
			return val11;
		}).Root<IDependency_1_1_1>();

	private void Setup_115() => DI.Setup()
		.Bind<IDependency_1_1_2_0>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_1_1_2_0>(out var val20);
			return val20;
		}).Root<IDependency_1_1_2_0>();

	private void Setup_116() => DI.Setup()
		.Bind<IDependency_1_1_2_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_1_1_2_1>(out var val21);
			return val21;
		}).Root<IDependency_1_1_2_1>();

	private void Setup_117() => DI.Setup()
		.Bind<IDependency_1_1_2_2>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_1_1_2_2>(out var val22);
			return val22;
		}).Root<IDependency_1_1_2_2>();

	private void Setup_118() => DI.Setup()
		.Bind<IDependency_1_1_2_3>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_1_1_2_3>(out var val23);
			return val23;
		}).Root<IDependency_1_1_2_3>();

	private void Setup_119() => DI.Setup()
		.Bind<IDependency_1_1_2>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_1_1_2>(out var val12);
			return val12;
		}).Root<IDependency_1_1_2>();

	private void Setup_120() => DI.Setup()
		.Bind<IDependency_1_1_3_0>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_1_1_3_0>(out var val20);
			return val20;
		}).Root<IDependency_1_1_3_0>();

	private void Setup_121() => DI.Setup()
		.Bind<IDependency_1_1_3_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_1_1_3_1>(out var val21);
			return val21;
		}).Root<IDependency_1_1_3_1>();

	private void Setup_122() => DI.Setup()
		.Bind<IDependency_1_1_3_2>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_1_1_3_2>(out var val22);
			return val22;
		}).Root<IDependency_1_1_3_2>();

	private void Setup_123() => DI.Setup()
		.Bind<IDependency_1_1_3_3>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_1_1_3_3>(out var val23);
			return val23;
		}).Root<IDependency_1_1_3_3>();

	private void Setup_124() => DI.Setup()
		.Bind<IDependency_1_1_3>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_1_1_3>(out var val13);
			return val13;
		}).Root<IDependency_1_1_3>();

	private void Setup_125() => DI.Setup()
		.Bind<IDependency_1_1>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_1_1>(out var val01);
			return val01;
		}).Root<IDependency_1_1>();

	private void Setup_126() => DI.Setup()
		.Bind<IDependency_1_2_0_0>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_1_2_0_0>(out var val20);
			return val20;
		}).Root<IDependency_1_2_0_0>();

	private void Setup_127() => DI.Setup()
		.Bind<IDependency_1_2_0_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_1_2_0_1>(out var val21);
			return val21;
		}).Root<IDependency_1_2_0_1>();

	private void Setup_128() => DI.Setup()
		.Bind<IDependency_1_2_0_2>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_1_2_0_2>(out var val22);
			return val22;
		}).Root<IDependency_1_2_0_2>();

	private void Setup_129() => DI.Setup()
		.Bind<IDependency_1_2_0_3>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_1_2_0_3>(out var val23);
			return val23;
		}).Root<IDependency_1_2_0_3>();

	private void Setup_130() => DI.Setup()
		.Bind<IDependency_1_2_0>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_1_2_0>(out var val10);
			return val10;
		}).Root<IDependency_1_2_0>();

	private void Setup_131() => DI.Setup()
		.Bind<IDependency_1_2_1_0>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_1_2_1_0>(out var val20);
			return val20;
		}).Root<IDependency_1_2_1_0>();

	private void Setup_132() => DI.Setup()
		.Bind<IDependency_1_2_1_1>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_1_2_1_1>(out var val21);
			return val21;
		}).Root<IDependency_1_2_1_1>();

	private void Setup_133() => DI.Setup()
		.Bind<IDependency_1_2_1_2>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_1_2_1_2>(out var val22);
			return val22;
		}).Root<IDependency_1_2_1_2>();

	private void Setup_134() => DI.Setup()
		.Bind<IDependency_1_2_1_3>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_1_2_1_3>(out var val23);
			return val23;
		}).Root<IDependency_1_2_1_3>();

	private void Setup_135() => DI.Setup()
		.Bind<IDependency_1_2_1>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_1_2_1>(out var val11);
			return val11;
		}).Root<IDependency_1_2_1>();

	private void Setup_136() => DI.Setup()
		.Bind<IDependency_1_2_2_0>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_1_2_2_0>(out var val20);
			return val20;
		}).Root<IDependency_1_2_2_0>();

	private void Setup_137() => DI.Setup()
		.Bind<IDependency_1_2_2_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_1_2_2_1>(out var val21);
			return val21;
		}).Root<IDependency_1_2_2_1>();

	private void Setup_138() => DI.Setup()
		.Bind<IDependency_1_2_2_2>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_1_2_2_2>(out var val22);
			return val22;
		}).Root<IDependency_1_2_2_2>();

	private void Setup_139() => DI.Setup()
		.Bind<IDependency_1_2_2_3>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_1_2_2_3>(out var val23);
			return val23;
		}).Root<IDependency_1_2_2_3>();

	private void Setup_140() => DI.Setup()
		.Bind<IDependency_1_2_2>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_1_2_2>(out var val12);
			return val12;
		}).Root<IDependency_1_2_2>();

	private void Setup_141() => DI.Setup()
		.Bind<IDependency_1_2_3_0>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_1_2_3_0>(out var val20);
			return val20;
		}).Root<IDependency_1_2_3_0>();

	private void Setup_142() => DI.Setup()
		.Bind<IDependency_1_2_3_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_1_2_3_1>(out var val21);
			return val21;
		}).Root<IDependency_1_2_3_1>();

	private void Setup_143() => DI.Setup()
		.Bind<IDependency_1_2_3_2>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_1_2_3_2>(out var val22);
			return val22;
		}).Root<IDependency_1_2_3_2>();

	private void Setup_144() => DI.Setup()
		.Bind<IDependency_1_2_3_3>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_1_2_3_3>(out var val23);
			return val23;
		}).Root<IDependency_1_2_3_3>();

	private void Setup_145() => DI.Setup()
		.Bind<IDependency_1_2_3>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_1_2_3>(out var val13);
			return val13;
		}).Root<IDependency_1_2_3>();

	private void Setup_146() => DI.Setup()
		.Bind<IDependency_1_2>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_1_2>(out var val02);
			return val02;
		}).Root<IDependency_1_2>();

	private void Setup_147() => DI.Setup()
		.Bind<IDependency_1_3_0_0>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_1_3_0_0>(out var val20);
			return val20;
		}).Root<IDependency_1_3_0_0>();

	private void Setup_148() => DI.Setup()
		.Bind<IDependency_1_3_0_1>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_1_3_0_1>(out var val21);
			return val21;
		}).Root<IDependency_1_3_0_1>();

	private void Setup_149() => DI.Setup()
		.Bind<IDependency_1_3_0_2>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_1_3_0_2>(out var val22);
			return val22;
		}).Root<IDependency_1_3_0_2>();

	private void Setup_150() => DI.Setup()
		.Bind<IDependency_1_3_0_3>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_1_3_0_3>(out var val23);
			return val23;
		}).Root<IDependency_1_3_0_3>();

	private void Setup_151() => DI.Setup()
		.Bind<IDependency_1_3_0>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_1_3_0>(out var val10);
			return val10;
		}).Root<IDependency_1_3_0>();

	private void Setup_152() => DI.Setup()
		.Bind<IDependency_1_3_1_0>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_1_3_1_0>(out var val20);
			return val20;
		}).Root<IDependency_1_3_1_0>();

	private void Setup_153() => DI.Setup()
		.Bind<IDependency_1_3_1_1>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_1_3_1_1>(out var val21);
			return val21;
		}).Root<IDependency_1_3_1_1>();

	private void Setup_154() => DI.Setup()
		.Bind<IDependency_1_3_1_2>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_1_3_1_2>(out var val22);
			return val22;
		}).Root<IDependency_1_3_1_2>();

	private void Setup_155() => DI.Setup()
		.Bind<IDependency_1_3_1_3>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_1_3_1_3>(out var val23);
			return val23;
		}).Root<IDependency_1_3_1_3>();

	private void Setup_156() => DI.Setup()
		.Bind<IDependency_1_3_1>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_1_3_1>(out var val11);
			return val11;
		}).Root<IDependency_1_3_1>();

	private void Setup_157() => DI.Setup()
		.Bind<IDependency_1_3_2_0>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_1_3_2_0>(out var val20);
			return val20;
		}).Root<IDependency_1_3_2_0>();

	private void Setup_158() => DI.Setup()
		.Bind<IDependency_1_3_2_1>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_1_3_2_1>(out var val21);
			return val21;
		}).Root<IDependency_1_3_2_1>();

	private void Setup_159() => DI.Setup()
		.Bind<IDependency_1_3_2_2>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_1_3_2_2>(out var val22);
			return val22;
		}).Root<IDependency_1_3_2_2>();

	private void Setup_160() => DI.Setup()
		.Bind<IDependency_1_3_2_3>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_1_3_2_3>(out var val23);
			return val23;
		}).Root<IDependency_1_3_2_3>();

	private void Setup_161() => DI.Setup()
		.Bind<IDependency_1_3_2>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_1_3_2>(out var val12);
			return val12;
		}).Root<IDependency_1_3_2>();

	private void Setup_162() => DI.Setup()
		.Bind<IDependency_1_3_3_0>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_1_3_3_0>(out var val20);
			return val20;
		}).Root<IDependency_1_3_3_0>();

	private void Setup_163() => DI.Setup()
		.Bind<IDependency_1_3_3_1>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_1_3_3_1>(out var val21);
			return val21;
		}).Root<IDependency_1_3_3_1>();

	private void Setup_164() => DI.Setup()
		.Bind<IDependency_1_3_3_2>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_1_3_3_2>(out var val22);
			return val22;
		}).Root<IDependency_1_3_3_2>();

	private void Setup_165() => DI.Setup()
		.Bind<IDependency_1_3_3_3>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_1_3_3_3>(out var val23);
			return val23;
		}).Root<IDependency_1_3_3_3>();

	private void Setup_166() => DI.Setup()
		.Bind<IDependency_1_3_3>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_1_3_3>(out var val13);
			return val13;
		}).Root<IDependency_1_3_3>();

	private void Setup_167() => DI.Setup()
		.Bind<IDependency_1_3>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_1_3>(out var val03);
			return val03;
		}).Root<IDependency_1_3>();

	private void Setup_168() => DI.Setup()
		.Bind<IDependency_2_0_0_0>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_2_0_0_0>(out var val20);
			return val20;
		}).Root<IDependency_2_0_0_0>();

	private void Setup_169() => DI.Setup()
		.Bind<IDependency_2_0_0_1>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_2_0_0_1>(out var val21);
			return val21;
		}).Root<IDependency_2_0_0_1>();

	private void Setup_170() => DI.Setup()
		.Bind<IDependency_2_0_0_2>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_2_0_0_2>(out var val22);
			return val22;
		}).Root<IDependency_2_0_0_2>();

	private void Setup_171() => DI.Setup()
		.Bind<IDependency_2_0_0_3>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_2_0_0_3>(out var val23);
			return val23;
		}).Root<IDependency_2_0_0_3>();

	private void Setup_172() => DI.Setup()
		.Bind<IDependency_2_0_0>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_2_0_0>(out var val10);
			return val10;
		}).Root<IDependency_2_0_0>();

	private void Setup_173() => DI.Setup()
		.Bind<IDependency_2_0_1_0>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_2_0_1_0>(out var val20);
			return val20;
		}).Root<IDependency_2_0_1_0>();

	private void Setup_174() => DI.Setup()
		.Bind<IDependency_2_0_1_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_2_0_1_1>(out var val21);
			return val21;
		}).Root<IDependency_2_0_1_1>();

	private void Setup_175() => DI.Setup()
		.Bind<IDependency_2_0_1_2>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_2_0_1_2>(out var val22);
			return val22;
		}).Root<IDependency_2_0_1_2>();

	private void Setup_176() => DI.Setup()
		.Bind<IDependency_2_0_1_3>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_2_0_1_3>(out var val23);
			return val23;
		}).Root<IDependency_2_0_1_3>();

	private void Setup_177() => DI.Setup()
		.Bind<IDependency_2_0_1>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_2_0_1>(out var val11);
			return val11;
		}).Root<IDependency_2_0_1>();

	private void Setup_178() => DI.Setup()
		.Bind<IDependency_2_0_2_0>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_2_0_2_0>(out var val20);
			return val20;
		}).Root<IDependency_2_0_2_0>();

	private void Setup_179() => DI.Setup()
		.Bind<IDependency_2_0_2_1>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_2_0_2_1>(out var val21);
			return val21;
		}).Root<IDependency_2_0_2_1>();

	private void Setup_180() => DI.Setup()
		.Bind<IDependency_2_0_2_2>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_2_0_2_2>(out var val22);
			return val22;
		}).Root<IDependency_2_0_2_2>();

	private void Setup_181() => DI.Setup()
		.Bind<IDependency_2_0_2_3>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_2_0_2_3>(out var val23);
			return val23;
		}).Root<IDependency_2_0_2_3>();

	private void Setup_182() => DI.Setup()
		.Bind<IDependency_2_0_2>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_2_0_2>(out var val12);
			return val12;
		}).Root<IDependency_2_0_2>();

	private void Setup_183() => DI.Setup()
		.Bind<IDependency_2_0_3_0>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_2_0_3_0>(out var val20);
			return val20;
		}).Root<IDependency_2_0_3_0>();

	private void Setup_184() => DI.Setup()
		.Bind<IDependency_2_0_3_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_2_0_3_1>(out var val21);
			return val21;
		}).Root<IDependency_2_0_3_1>();

	private void Setup_185() => DI.Setup()
		.Bind<IDependency_2_0_3_2>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_2_0_3_2>(out var val22);
			return val22;
		}).Root<IDependency_2_0_3_2>();

	private void Setup_186() => DI.Setup()
		.Bind<IDependency_2_0_3_3>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_2_0_3_3>(out var val23);
			return val23;
		}).Root<IDependency_2_0_3_3>();

	private void Setup_187() => DI.Setup()
		.Bind<IDependency_2_0_3>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_2_0_3>(out var val13);
			return val13;
		}).Root<IDependency_2_0_3>();

	private void Setup_188() => DI.Setup()
		.Bind<IDependency_2_0>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_2_0>(out var val00);
			return val00;
		}).Root<IDependency_2_0>();

	private void Setup_189() => DI.Setup()
		.Bind<IDependency_2_1_0_0>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_2_1_0_0>(out var val20);
			return val20;
		}).Root<IDependency_2_1_0_0>();

	private void Setup_190() => DI.Setup()
		.Bind<IDependency_2_1_0_1>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_2_1_0_1>(out var val21);
			return val21;
		}).Root<IDependency_2_1_0_1>();

	private void Setup_191() => DI.Setup()
		.Bind<IDependency_2_1_0_2>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_2_1_0_2>(out var val22);
			return val22;
		}).Root<IDependency_2_1_0_2>();

	private void Setup_192() => DI.Setup()
		.Bind<IDependency_2_1_0_3>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_2_1_0_3>(out var val23);
			return val23;
		}).Root<IDependency_2_1_0_3>();

	private void Setup_193() => DI.Setup()
		.Bind<IDependency_2_1_0>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_2_1_0>(out var val10);
			return val10;
		}).Root<IDependency_2_1_0>();

	private void Setup_194() => DI.Setup()
		.Bind<IDependency_2_1_1_0>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_2_1_1_0>(out var val20);
			return val20;
		}).Root<IDependency_2_1_1_0>();

	private void Setup_195() => DI.Setup()
		.Bind<IDependency_2_1_1_1>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_2_1_1_1>(out var val21);
			return val21;
		}).Root<IDependency_2_1_1_1>();

	private void Setup_196() => DI.Setup()
		.Bind<IDependency_2_1_1_2>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_2_1_1_2>(out var val22);
			return val22;
		}).Root<IDependency_2_1_1_2>();

	private void Setup_197() => DI.Setup()
		.Bind<IDependency_2_1_1_3>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_2_1_1_3>(out var val23);
			return val23;
		}).Root<IDependency_2_1_1_3>();

	private void Setup_198() => DI.Setup()
		.Bind<IDependency_2_1_1>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_2_1_1>(out var val11);
			return val11;
		}).Root<IDependency_2_1_1>();

	private void Setup_199() => DI.Setup()
		.Bind<IDependency_2_1_2_0>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_2_1_2_0>(out var val20);
			return val20;
		}).Root<IDependency_2_1_2_0>();

	private void Setup_200() => DI.Setup()
		.Bind<IDependency_2_1_2_1>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_2_1_2_1>(out var val21);
			return val21;
		}).Root<IDependency_2_1_2_1>();

	private void Setup_201() => DI.Setup()
		.Bind<IDependency_2_1_2_2>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_2_1_2_2>(out var val22);
			return val22;
		}).Root<IDependency_2_1_2_2>();

	private void Setup_202() => DI.Setup()
		.Bind<IDependency_2_1_2_3>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_2_1_2_3>(out var val23);
			return val23;
		}).Root<IDependency_2_1_2_3>();

	private void Setup_203() => DI.Setup()
		.Bind<IDependency_2_1_2>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_2_1_2>(out var val12);
			return val12;
		}).Root<IDependency_2_1_2>();

	private void Setup_204() => DI.Setup()
		.Bind<IDependency_2_1_3_0>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_2_1_3_0>(out var val20);
			return val20;
		}).Root<IDependency_2_1_3_0>();

	private void Setup_205() => DI.Setup()
		.Bind<IDependency_2_1_3_1>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_2_1_3_1>(out var val21);
			return val21;
		}).Root<IDependency_2_1_3_1>();

	private void Setup_206() => DI.Setup()
		.Bind<IDependency_2_1_3_2>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_2_1_3_2>(out var val22);
			return val22;
		}).Root<IDependency_2_1_3_2>();

	private void Setup_207() => DI.Setup()
		.Bind<IDependency_2_1_3_3>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_2_1_3_3>(out var val23);
			return val23;
		}).Root<IDependency_2_1_3_3>();

	private void Setup_208() => DI.Setup()
		.Bind<IDependency_2_1_3>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_2_1_3>(out var val13);
			return val13;
		}).Root<IDependency_2_1_3>();

	private void Setup_209() => DI.Setup()
		.Bind<IDependency_2_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_2_1>(out var val01);
			return val01;
		}).Root<IDependency_2_1>();

	private void Setup_210() => DI.Setup()
		.Bind<IDependency_2_2_0_0>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_2_2_0_0>(out var val20);
			return val20;
		}).Root<IDependency_2_2_0_0>();

	private void Setup_211() => DI.Setup()
		.Bind<IDependency_2_2_0_1>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_2_2_0_1>(out var val21);
			return val21;
		}).Root<IDependency_2_2_0_1>();

	private void Setup_212() => DI.Setup()
		.Bind<IDependency_2_2_0_2>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_2_2_0_2>(out var val22);
			return val22;
		}).Root<IDependency_2_2_0_2>();

	private void Setup_213() => DI.Setup()
		.Bind<IDependency_2_2_0_3>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_2_2_0_3>(out var val23);
			return val23;
		}).Root<IDependency_2_2_0_3>();

	private void Setup_214() => DI.Setup()
		.Bind<IDependency_2_2_0>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_2_2_0>(out var val10);
			return val10;
		}).Root<IDependency_2_2_0>();

	private void Setup_215() => DI.Setup()
		.Bind<IDependency_2_2_1_0>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_2_2_1_0>(out var val20);
			return val20;
		}).Root<IDependency_2_2_1_0>();

	private void Setup_216() => DI.Setup()
		.Bind<IDependency_2_2_1_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_2_2_1_1>(out var val21);
			return val21;
		}).Root<IDependency_2_2_1_1>();

	private void Setup_217() => DI.Setup()
		.Bind<IDependency_2_2_1_2>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_2_2_1_2>(out var val22);
			return val22;
		}).Root<IDependency_2_2_1_2>();

	private void Setup_218() => DI.Setup()
		.Bind<IDependency_2_2_1_3>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_2_2_1_3>(out var val23);
			return val23;
		}).Root<IDependency_2_2_1_3>();

	private void Setup_219() => DI.Setup()
		.Bind<IDependency_2_2_1>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_2_2_1>(out var val11);
			return val11;
		}).Root<IDependency_2_2_1>();

	private void Setup_220() => DI.Setup()
		.Bind<IDependency_2_2_2_0>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_2_2_2_0>(out var val20);
			return val20;
		}).Root<IDependency_2_2_2_0>();

	private void Setup_221() => DI.Setup()
		.Bind<IDependency_2_2_2_1>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_2_2_2_1>(out var val21);
			return val21;
		}).Root<IDependency_2_2_2_1>();

	private void Setup_222() => DI.Setup()
		.Bind<IDependency_2_2_2_2>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_2_2_2_2>(out var val22);
			return val22;
		}).Root<IDependency_2_2_2_2>();

	private void Setup_223() => DI.Setup()
		.Bind<IDependency_2_2_2_3>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_2_2_2_3>(out var val23);
			return val23;
		}).Root<IDependency_2_2_2_3>();

	private void Setup_224() => DI.Setup()
		.Bind<IDependency_2_2_2>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_2_2_2>(out var val12);
			return val12;
		}).Root<IDependency_2_2_2>();

	private void Setup_225() => DI.Setup()
		.Bind<IDependency_2_2_3_0>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_2_2_3_0>(out var val20);
			return val20;
		}).Root<IDependency_2_2_3_0>();

	private void Setup_226() => DI.Setup()
		.Bind<IDependency_2_2_3_1>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_2_2_3_1>(out var val21);
			return val21;
		}).Root<IDependency_2_2_3_1>();

	private void Setup_227() => DI.Setup()
		.Bind<IDependency_2_2_3_2>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_2_2_3_2>(out var val22);
			return val22;
		}).Root<IDependency_2_2_3_2>();

	private void Setup_228() => DI.Setup()
		.Bind<IDependency_2_2_3_3>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_2_2_3_3>(out var val23);
			return val23;
		}).Root<IDependency_2_2_3_3>();

	private void Setup_229() => DI.Setup()
		.Bind<IDependency_2_2_3>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_2_2_3>(out var val13);
			return val13;
		}).Root<IDependency_2_2_3>();

	private void Setup_230() => DI.Setup()
		.Bind<IDependency_2_2>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_2_2>(out var val02);
			return val02;
		}).Root<IDependency_2_2>();

	private void Setup_231() => DI.Setup()
		.Bind<IDependency_2_3_0_0>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_2_3_0_0>(out var val20);
			return val20;
		}).Root<IDependency_2_3_0_0>();

	private void Setup_232() => DI.Setup()
		.Bind<IDependency_2_3_0_1>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_2_3_0_1>(out var val21);
			return val21;
		}).Root<IDependency_2_3_0_1>();

	private void Setup_233() => DI.Setup()
		.Bind<IDependency_2_3_0_2>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_2_3_0_2>(out var val22);
			return val22;
		}).Root<IDependency_2_3_0_2>();

	private void Setup_234() => DI.Setup()
		.Bind<IDependency_2_3_0_3>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_2_3_0_3>(out var val23);
			return val23;
		}).Root<IDependency_2_3_0_3>();

	private void Setup_235() => DI.Setup()
		.Bind<IDependency_2_3_0>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_2_3_0>(out var val10);
			return val10;
		}).Root<IDependency_2_3_0>();

	private void Setup_236() => DI.Setup()
		.Bind<IDependency_2_3_1_0>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_2_3_1_0>(out var val20);
			return val20;
		}).Root<IDependency_2_3_1_0>();

	private void Setup_237() => DI.Setup()
		.Bind<IDependency_2_3_1_1>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_2_3_1_1>(out var val21);
			return val21;
		}).Root<IDependency_2_3_1_1>();

	private void Setup_238() => DI.Setup()
		.Bind<IDependency_2_3_1_2>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_2_3_1_2>(out var val22);
			return val22;
		}).Root<IDependency_2_3_1_2>();

	private void Setup_239() => DI.Setup()
		.Bind<IDependency_2_3_1_3>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_2_3_1_3>(out var val23);
			return val23;
		}).Root<IDependency_2_3_1_3>();

	private void Setup_240() => DI.Setup()
		.Bind<IDependency_2_3_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_2_3_1>(out var val11);
			return val11;
		}).Root<IDependency_2_3_1>();

	private void Setup_241() => DI.Setup()
		.Bind<IDependency_2_3_2_0>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_2_3_2_0>(out var val20);
			return val20;
		}).Root<IDependency_2_3_2_0>();

	private void Setup_242() => DI.Setup()
		.Bind<IDependency_2_3_2_1>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_2_3_2_1>(out var val21);
			return val21;
		}).Root<IDependency_2_3_2_1>();

	private void Setup_243() => DI.Setup()
		.Bind<IDependency_2_3_2_2>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_2_3_2_2>(out var val22);
			return val22;
		}).Root<IDependency_2_3_2_2>();

	private void Setup_244() => DI.Setup()
		.Bind<IDependency_2_3_2_3>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_2_3_2_3>(out var val23);
			return val23;
		}).Root<IDependency_2_3_2_3>();

	private void Setup_245() => DI.Setup()
		.Bind<IDependency_2_3_2>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_2_3_2>(out var val12);
			return val12;
		}).Root<IDependency_2_3_2>();

	private void Setup_246() => DI.Setup()
		.Bind<IDependency_2_3_3_0>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_2_3_3_0>(out var val20);
			return val20;
		}).Root<IDependency_2_3_3_0>();

	private void Setup_247() => DI.Setup()
		.Bind<IDependency_2_3_3_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_2_3_3_1>(out var val21);
			return val21;
		}).Root<IDependency_2_3_3_1>();

	private void Setup_248() => DI.Setup()
		.Bind<IDependency_2_3_3_2>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_2_3_3_2>(out var val22);
			return val22;
		}).Root<IDependency_2_3_3_2>();

	private void Setup_249() => DI.Setup()
		.Bind<IDependency_2_3_3_3>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_2_3_3_3>(out var val23);
			return val23;
		}).Root<IDependency_2_3_3_3>();

	private void Setup_250() => DI.Setup()
		.Bind<IDependency_2_3_3>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_2_3_3>(out var val13);
			return val13;
		}).Root<IDependency_2_3_3>();

	private void Setup_251() => DI.Setup()
		.Bind<IDependency_2_3>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_2_3>(out var val03);
			return val03;
		}).Root<IDependency_2_3>();

	private void Setup_252() => DI.Setup()
		.Bind<IDependency_3_0_0_0>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_3_0_0_0>(out var val20);
			return val20;
		}).Root<IDependency_3_0_0_0>();

	private void Setup_253() => DI.Setup()
		.Bind<IDependency_3_0_0_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_3_0_0_1>(out var val21);
			return val21;
		}).Root<IDependency_3_0_0_1>();

	private void Setup_254() => DI.Setup()
		.Bind<IDependency_3_0_0_2>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_3_0_0_2>(out var val22);
			return val22;
		}).Root<IDependency_3_0_0_2>();

	private void Setup_255() => DI.Setup()
		.Bind<IDependency_3_0_0_3>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_3_0_0_3>(out var val23);
			return val23;
		}).Root<IDependency_3_0_0_3>();

	private void Setup_256() => DI.Setup()
		.Bind<IDependency_3_0_0>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_3_0_0>(out var val10);
			return val10;
		}).Root<IDependency_3_0_0>();

	private void Setup_257() => DI.Setup()
		.Bind<IDependency_3_0_1_0>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_3_0_1_0>(out var val20);
			return val20;
		}).Root<IDependency_3_0_1_0>();

	private void Setup_258() => DI.Setup()
		.Bind<IDependency_3_0_1_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_3_0_1_1>(out var val21);
			return val21;
		}).Root<IDependency_3_0_1_1>();

	private void Setup_259() => DI.Setup()
		.Bind<IDependency_3_0_1_2>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_3_0_1_2>(out var val22);
			return val22;
		}).Root<IDependency_3_0_1_2>();

	private void Setup_260() => DI.Setup()
		.Bind<IDependency_3_0_1_3>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_3_0_1_3>(out var val23);
			return val23;
		}).Root<IDependency_3_0_1_3>();

	private void Setup_261() => DI.Setup()
		.Bind<IDependency_3_0_1>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_3_0_1>(out var val11);
			return val11;
		}).Root<IDependency_3_0_1>();

	private void Setup_262() => DI.Setup()
		.Bind<IDependency_3_0_2_0>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_3_0_2_0>(out var val20);
			return val20;
		}).Root<IDependency_3_0_2_0>();

	private void Setup_263() => DI.Setup()
		.Bind<IDependency_3_0_2_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_3_0_2_1>(out var val21);
			return val21;
		}).Root<IDependency_3_0_2_1>();

	private void Setup_264() => DI.Setup()
		.Bind<IDependency_3_0_2_2>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_3_0_2_2>(out var val22);
			return val22;
		}).Root<IDependency_3_0_2_2>();

	private void Setup_265() => DI.Setup()
		.Bind<IDependency_3_0_2_3>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_3_0_2_3>(out var val23);
			return val23;
		}).Root<IDependency_3_0_2_3>();

	private void Setup_266() => DI.Setup()
		.Bind<IDependency_3_0_2>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_3_0_2>(out var val12);
			return val12;
		}).Root<IDependency_3_0_2>();

	private void Setup_267() => DI.Setup()
		.Bind<IDependency_3_0_3_0>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_3_0_3_0>(out var val20);
			return val20;
		}).Root<IDependency_3_0_3_0>();

	private void Setup_268() => DI.Setup()
		.Bind<IDependency_3_0_3_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_3_0_3_1>(out var val21);
			return val21;
		}).Root<IDependency_3_0_3_1>();

	private void Setup_269() => DI.Setup()
		.Bind<IDependency_3_0_3_2>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_3_0_3_2>(out var val22);
			return val22;
		}).Root<IDependency_3_0_3_2>();

	private void Setup_270() => DI.Setup()
		.Bind<IDependency_3_0_3_3>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_3_0_3_3>(out var val23);
			return val23;
		}).Root<IDependency_3_0_3_3>();

	private void Setup_271() => DI.Setup()
		.Bind<IDependency_3_0_3>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_3_0_3>(out var val13);
			return val13;
		}).Root<IDependency_3_0_3>();

	private void Setup_272() => DI.Setup()
		.Bind<IDependency_3_0>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_3_0>(out var val00);
			return val00;
		}).Root<IDependency_3_0>();

	private void Setup_273() => DI.Setup()
		.Bind<IDependency_3_1_0_0>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_3_1_0_0>(out var val20);
			return val20;
		}).Root<IDependency_3_1_0_0>();

	private void Setup_274() => DI.Setup()
		.Bind<IDependency_3_1_0_1>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_3_1_0_1>(out var val21);
			return val21;
		}).Root<IDependency_3_1_0_1>();

	private void Setup_275() => DI.Setup()
		.Bind<IDependency_3_1_0_2>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_3_1_0_2>(out var val22);
			return val22;
		}).Root<IDependency_3_1_0_2>();

	private void Setup_276() => DI.Setup()
		.Bind<IDependency_3_1_0_3>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_3_1_0_3>(out var val23);
			return val23;
		}).Root<IDependency_3_1_0_3>();

	private void Setup_277() => DI.Setup()
		.Bind<IDependency_3_1_0>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_3_1_0>(out var val10);
			return val10;
		}).Root<IDependency_3_1_0>();

	private void Setup_278() => DI.Setup()
		.Bind<IDependency_3_1_1_0>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_3_1_1_0>(out var val20);
			return val20;
		}).Root<IDependency_3_1_1_0>();

	private void Setup_279() => DI.Setup()
		.Bind<IDependency_3_1_1_1>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_3_1_1_1>(out var val21);
			return val21;
		}).Root<IDependency_3_1_1_1>();

	private void Setup_280() => DI.Setup()
		.Bind<IDependency_3_1_1_2>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_3_1_1_2>(out var val22);
			return val22;
		}).Root<IDependency_3_1_1_2>();

	private void Setup_281() => DI.Setup()
		.Bind<IDependency_3_1_1_3>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_3_1_1_3>(out var val23);
			return val23;
		}).Root<IDependency_3_1_1_3>();

	private void Setup_282() => DI.Setup()
		.Bind<IDependency_3_1_1>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_3_1_1>(out var val11);
			return val11;
		}).Root<IDependency_3_1_1>();

	private void Setup_283() => DI.Setup()
		.Bind<IDependency_3_1_2_0>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_3_1_2_0>(out var val20);
			return val20;
		}).Root<IDependency_3_1_2_0>();

	private void Setup_284() => DI.Setup()
		.Bind<IDependency_3_1_2_1>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_3_1_2_1>(out var val21);
			return val21;
		}).Root<IDependency_3_1_2_1>();

	private void Setup_285() => DI.Setup()
		.Bind<IDependency_3_1_2_2>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_3_1_2_2>(out var val22);
			return val22;
		}).Root<IDependency_3_1_2_2>();

	private void Setup_286() => DI.Setup()
		.Bind<IDependency_3_1_2_3>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_3_1_2_3>(out var val23);
			return val23;
		}).Root<IDependency_3_1_2_3>();

	private void Setup_287() => DI.Setup()
		.Bind<IDependency_3_1_2>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_3_1_2>(out var val12);
			return val12;
		}).Root<IDependency_3_1_2>();

	private void Setup_288() => DI.Setup()
		.Bind<IDependency_3_1_3_0>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_3_1_3_0>(out var val20);
			return val20;
		}).Root<IDependency_3_1_3_0>();

	private void Setup_289() => DI.Setup()
		.Bind<IDependency_3_1_3_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_3_1_3_1>(out var val21);
			return val21;
		}).Root<IDependency_3_1_3_1>();

	private void Setup_290() => DI.Setup()
		.Bind<IDependency_3_1_3_2>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_3_1_3_2>(out var val22);
			return val22;
		}).Root<IDependency_3_1_3_2>();

	private void Setup_291() => DI.Setup()
		.Bind<IDependency_3_1_3_3>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_3_1_3_3>(out var val23);
			return val23;
		}).Root<IDependency_3_1_3_3>();

	private void Setup_292() => DI.Setup()
		.Bind<IDependency_3_1_3>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_3_1_3>(out var val13);
			return val13;
		}).Root<IDependency_3_1_3>();

	private void Setup_293() => DI.Setup()
		.Bind<IDependency_3_1>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_3_1>(out var val01);
			return val01;
		}).Root<IDependency_3_1>();

	private void Setup_294() => DI.Setup()
		.Bind<IDependency_3_2_0_0>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_3_2_0_0>(out var val20);
			return val20;
		}).Root<IDependency_3_2_0_0>();

	private void Setup_295() => DI.Setup()
		.Bind<IDependency_3_2_0_1>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_3_2_0_1>(out var val21);
			return val21;
		}).Root<IDependency_3_2_0_1>();

	private void Setup_296() => DI.Setup()
		.Bind<IDependency_3_2_0_2>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_3_2_0_2>(out var val22);
			return val22;
		}).Root<IDependency_3_2_0_2>();

	private void Setup_297() => DI.Setup()
		.Bind<IDependency_3_2_0_3>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_3_2_0_3>(out var val23);
			return val23;
		}).Root<IDependency_3_2_0_3>();

	private void Setup_298() => DI.Setup()
		.Bind<IDependency_3_2_0>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_3_2_0>(out var val10);
			return val10;
		}).Root<IDependency_3_2_0>();

	private void Setup_299() => DI.Setup()
		.Bind<IDependency_3_2_1_0>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_3_2_1_0>(out var val20);
			return val20;
		}).Root<IDependency_3_2_1_0>();

	private void Setup_300() => DI.Setup()
		.Bind<IDependency_3_2_1_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_3_2_1_1>(out var val21);
			return val21;
		}).Root<IDependency_3_2_1_1>();

	private void Setup_301() => DI.Setup()
		.Bind<IDependency_3_2_1_2>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_3_2_1_2>(out var val22);
			return val22;
		}).Root<IDependency_3_2_1_2>();

	private void Setup_302() => DI.Setup()
		.Bind<IDependency_3_2_1_3>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_3_2_1_3>(out var val23);
			return val23;
		}).Root<IDependency_3_2_1_3>();

	private void Setup_303() => DI.Setup()
		.Bind<IDependency_3_2_1>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_3_2_1>(out var val11);
			return val11;
		}).Root<IDependency_3_2_1>();

	private void Setup_304() => DI.Setup()
		.Bind<IDependency_3_2_2_0>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_3_2_2_0>(out var val20);
			return val20;
		}).Root<IDependency_3_2_2_0>();

	private void Setup_305() => DI.Setup()
		.Bind<IDependency_3_2_2_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_3_2_2_1>(out var val21);
			return val21;
		}).Root<IDependency_3_2_2_1>();

	private void Setup_306() => DI.Setup()
		.Bind<IDependency_3_2_2_2>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_3_2_2_2>(out var val22);
			return val22;
		}).Root<IDependency_3_2_2_2>();

	private void Setup_307() => DI.Setup()
		.Bind<IDependency_3_2_2_3>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_3_2_2_3>(out var val23);
			return val23;
		}).Root<IDependency_3_2_2_3>();

	private void Setup_308() => DI.Setup()
		.Bind<IDependency_3_2_2>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_3_2_2>(out var val12);
			return val12;
		}).Root<IDependency_3_2_2>();

	private void Setup_309() => DI.Setup()
		.Bind<IDependency_3_2_3_0>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_3_2_3_0>(out var val20);
			return val20;
		}).Root<IDependency_3_2_3_0>();

	private void Setup_310() => DI.Setup()
		.Bind<IDependency_3_2_3_1>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_3_2_3_1>(out var val21);
			return val21;
		}).Root<IDependency_3_2_3_1>();

	private void Setup_311() => DI.Setup()
		.Bind<IDependency_3_2_3_2>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_3_2_3_2>(out var val22);
			return val22;
		}).Root<IDependency_3_2_3_2>();

	private void Setup_312() => DI.Setup()
		.Bind<IDependency_3_2_3_3>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_3_2_3_3>(out var val23);
			return val23;
		}).Root<IDependency_3_2_3_3>();

	private void Setup_313() => DI.Setup()
		.Bind<IDependency_3_2_3>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_3_2_3>(out var val13);
			return val13;
		}).Root<IDependency_3_2_3>();

	private void Setup_314() => DI.Setup()
		.Bind<IDependency_3_2>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_3_2>(out var val02);
			return val02;
		}).Root<IDependency_3_2>();

	private void Setup_315() => DI.Setup()
		.Bind<IDependency_3_3_0_0>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_3_3_0_0>(out var val20);
			return val20;
		}).Root<IDependency_3_3_0_0>();

	private void Setup_316() => DI.Setup()
		.Bind<IDependency_3_3_0_1>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_3_3_0_1>(out var val21);
			return val21;
		}).Root<IDependency_3_3_0_1>();

	private void Setup_317() => DI.Setup()
		.Bind<IDependency_3_3_0_2>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_3_3_0_2>(out var val22);
			return val22;
		}).Root<IDependency_3_3_0_2>();

	private void Setup_318() => DI.Setup()
		.Bind<IDependency_3_3_0_3>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_3_3_0_3>(out var val23);
			return val23;
		}).Root<IDependency_3_3_0_3>();

	private void Setup_319() => DI.Setup()
		.Bind<IDependency_3_3_0>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_3_3_0>(out var val10);
			return val10;
		}).Root<IDependency_3_3_0>();

	private void Setup_320() => DI.Setup()
		.Bind<IDependency_3_3_1_0>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_3_3_1_0>(out var val20);
			return val20;
		}).Root<IDependency_3_3_1_0>();

	private void Setup_321() => DI.Setup()
		.Bind<IDependency_3_3_1_1>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_3_3_1_1>(out var val21);
			return val21;
		}).Root<IDependency_3_3_1_1>();

	private void Setup_322() => DI.Setup()
		.Bind<IDependency_3_3_1_2>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_3_3_1_2>(out var val22);
			return val22;
		}).Root<IDependency_3_3_1_2>();

	private void Setup_323() => DI.Setup()
		.Bind<IDependency_3_3_1_3>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_3_3_1_3>(out var val23);
			return val23;
		}).Root<IDependency_3_3_1_3>();

	private void Setup_324() => DI.Setup()
		.Bind<IDependency_3_3_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_3_3_1>(out var val11);
			return val11;
		}).Root<IDependency_3_3_1>();

	private void Setup_325() => DI.Setup()
		.Bind<IDependency_3_3_2_0>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_3_3_2_0>(out var val20);
			return val20;
		}).Root<IDependency_3_3_2_0>();

	private void Setup_326() => DI.Setup()
		.Bind<IDependency_3_3_2_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_3_3_2_1>(out var val21);
			return val21;
		}).Root<IDependency_3_3_2_1>();

	private void Setup_327() => DI.Setup()
		.Bind<IDependency_3_3_2_2>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_3_3_2_2>(out var val22);
			return val22;
		}).Root<IDependency_3_3_2_2>();

	private void Setup_328() => DI.Setup()
		.Bind<IDependency_3_3_2_3>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_3_3_2_3>(out var val23);
			return val23;
		}).Root<IDependency_3_3_2_3>();

	private void Setup_329() => DI.Setup()
		.Bind<IDependency_3_3_2>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_3_3_2>(out var val12);
			return val12;
		}).Root<IDependency_3_3_2>();

	private void Setup_330() => DI.Setup()
		.Bind<IDependency_3_3_3_0>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_3_3_3_0>(out var val20);
			return val20;
		}).Root<IDependency_3_3_3_0>();

	private void Setup_331() => DI.Setup()
		.Bind<IDependency_3_3_3_1>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_3_3_3_1>(out var val21);
			return val21;
		}).Root<IDependency_3_3_3_1>();

	private void Setup_332() => DI.Setup()
		.Bind<IDependency_3_3_3_2>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_3_3_3_2>(out var val22);
			return val22;
		}).Root<IDependency_3_3_3_2>();

	private void Setup_333() => DI.Setup()
		.Bind<IDependency_3_3_3_3>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_3_3_3_3>(out var val23);
			return val23;
		}).Root<IDependency_3_3_3_3>();

	private void Setup_334() => DI.Setup()
		.Bind<IDependency_3_3_3>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_3_3_3>(out var val13);
			return val13;
		}).Root<IDependency_3_3_3>();

	private void Setup_335() => DI.Setup()
		.Bind<IDependency_3_3>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_3_3>(out var val03);
			return val03;
		}).Root<IDependency_3_3>();

	private void Setup_336() => DI.Setup()
		.Bind<IDependency_0>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_0>(out var val0);
			return val0;
		}).Root<IDependency_0>();

	private void Setup_337() => DI.Setup()
		.Bind<IDependency_1>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_1>(out var val1);
			return val1;
		}).Root<IDependency_1>();

	private void Setup_338() => DI.Setup()
		.Bind<IDependency_2>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_2>(out var val2);
			return val2;
		}).Root<IDependency_2>();

	private void Setup_339() => DI.Setup()
		.Bind<IDependency_3>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_3>(out var val3);
			return val3;
		}).Root<IDependency_3>();

}

public interface IRoot;

public class Root(IDependency_0 dep0, IDependency_1 dep1, IDependency_2 dep2, IDependency_3 dep3): IRoot;

public interface IDependency_0;

public class Dependency_0(IDependency_0_0 dep00, IDependency_0_1 dep01, IDependency_0_2 dep02, IDependency_0_3 dep03): IDependency_0;

public interface IDependency_0_0;

public class Dependency_0_0(IDependency_0_0_0 dep10, IDependency_0_0_1 dep11, IDependency_0_0_2 dep12, IDependency_0_0_3 dep13): IDependency_0_0;

public interface IDependency_0_0_0;

public class Dependency_0_0_0(IDependency_0_0_0_0 dep20, IDependency_0_0_0_1 dep21, IDependency_0_0_0_2 dep22, IDependency_0_0_0_3 dep23): IDependency_0_0_0;

public interface IDependency_0_0_0_0;

public class Dependency_0_0_0_0(): IDependency_0_0_0_0;

public interface IDependency_0_0_0_1;

public class Dependency_0_0_0_1(): IDependency_0_0_0_1;

public interface IDependency_0_0_0_2;

public class Dependency_0_0_0_2(): IDependency_0_0_0_2;

public interface IDependency_0_0_0_3;

public class Dependency_0_0_0_3(): IDependency_0_0_0_3;

public interface IDependency_0_0_1;

public class Dependency_0_0_1(IDependency_0_0_1_0 dep20, IDependency_0_0_1_1 dep21, IDependency_0_0_1_2 dep22, IDependency_0_0_1_3 dep23): IDependency_0_0_1;

public interface IDependency_0_0_1_0;

public class Dependency_0_0_1_0(): IDependency_0_0_1_0;

public interface IDependency_0_0_1_1;

public class Dependency_0_0_1_1(): IDependency_0_0_1_1;

public interface IDependency_0_0_1_2;

public class Dependency_0_0_1_2(): IDependency_0_0_1_2;

public interface IDependency_0_0_1_3;

public class Dependency_0_0_1_3(): IDependency_0_0_1_3;

public interface IDependency_0_0_2;

public class Dependency_0_0_2(IDependency_0_0_2_0 dep20, IDependency_0_0_2_1 dep21, IDependency_0_0_2_2 dep22, IDependency_0_0_2_3 dep23): IDependency_0_0_2;

public interface IDependency_0_0_2_0;

public class Dependency_0_0_2_0(): IDependency_0_0_2_0;

public interface IDependency_0_0_2_1;

public class Dependency_0_0_2_1(): IDependency_0_0_2_1;

public interface IDependency_0_0_2_2;

public class Dependency_0_0_2_2(): IDependency_0_0_2_2;

public interface IDependency_0_0_2_3;

public class Dependency_0_0_2_3(): IDependency_0_0_2_3;

public interface IDependency_0_0_3;

public class Dependency_0_0_3(IDependency_0_0_3_0 dep20, IDependency_0_0_3_1 dep21, IDependency_0_0_3_2 dep22, IDependency_0_0_3_3 dep23): IDependency_0_0_3;

public interface IDependency_0_0_3_0;

public class Dependency_0_0_3_0(): IDependency_0_0_3_0;

public interface IDependency_0_0_3_1;

public class Dependency_0_0_3_1(): IDependency_0_0_3_1;

public interface IDependency_0_0_3_2;

public class Dependency_0_0_3_2(): IDependency_0_0_3_2;

public interface IDependency_0_0_3_3;

public class Dependency_0_0_3_3(): IDependency_0_0_3_3;

public interface IDependency_0_1;

public class Dependency_0_1(IDependency_0_1_0 dep10, IDependency_0_1_1 dep11, IDependency_0_1_2 dep12, IDependency_0_1_3 dep13): IDependency_0_1;

public interface IDependency_0_1_0;

public class Dependency_0_1_0(IDependency_0_1_0_0 dep20, IDependency_0_1_0_1 dep21, IDependency_0_1_0_2 dep22, IDependency_0_1_0_3 dep23): IDependency_0_1_0;

public interface IDependency_0_1_0_0;

public class Dependency_0_1_0_0(): IDependency_0_1_0_0;

public interface IDependency_0_1_0_1;

public class Dependency_0_1_0_1(): IDependency_0_1_0_1;

public interface IDependency_0_1_0_2;

public class Dependency_0_1_0_2(): IDependency_0_1_0_2;

public interface IDependency_0_1_0_3;

public class Dependency_0_1_0_3(): IDependency_0_1_0_3;

public interface IDependency_0_1_1;

public class Dependency_0_1_1(IDependency_0_1_1_0 dep20, IDependency_0_1_1_1 dep21, IDependency_0_1_1_2 dep22, IDependency_0_1_1_3 dep23): IDependency_0_1_1;

public interface IDependency_0_1_1_0;

public class Dependency_0_1_1_0(): IDependency_0_1_1_0;

public interface IDependency_0_1_1_1;

public class Dependency_0_1_1_1(): IDependency_0_1_1_1;

public interface IDependency_0_1_1_2;

public class Dependency_0_1_1_2(): IDependency_0_1_1_2;

public interface IDependency_0_1_1_3;

public class Dependency_0_1_1_3(): IDependency_0_1_1_3;

public interface IDependency_0_1_2;

public class Dependency_0_1_2(IDependency_0_1_2_0 dep20, IDependency_0_1_2_1 dep21, IDependency_0_1_2_2 dep22, IDependency_0_1_2_3 dep23): IDependency_0_1_2;

public interface IDependency_0_1_2_0;

public class Dependency_0_1_2_0(): IDependency_0_1_2_0;

public interface IDependency_0_1_2_1;

public class Dependency_0_1_2_1(): IDependency_0_1_2_1;

public interface IDependency_0_1_2_2;

public class Dependency_0_1_2_2(): IDependency_0_1_2_2;

public interface IDependency_0_1_2_3;

public class Dependency_0_1_2_3(): IDependency_0_1_2_3;

public interface IDependency_0_1_3;

public class Dependency_0_1_3(IDependency_0_1_3_0 dep20, IDependency_0_1_3_1 dep21, IDependency_0_1_3_2 dep22, IDependency_0_1_3_3 dep23): IDependency_0_1_3;

public interface IDependency_0_1_3_0;

public class Dependency_0_1_3_0(): IDependency_0_1_3_0;

public interface IDependency_0_1_3_1;

public class Dependency_0_1_3_1(): IDependency_0_1_3_1;

public interface IDependency_0_1_3_2;

public class Dependency_0_1_3_2(): IDependency_0_1_3_2;

public interface IDependency_0_1_3_3;

public class Dependency_0_1_3_3(): IDependency_0_1_3_3;

public interface IDependency_0_2;

public class Dependency_0_2(IDependency_0_2_0 dep10, IDependency_0_2_1 dep11, IDependency_0_2_2 dep12, IDependency_0_2_3 dep13): IDependency_0_2;

public interface IDependency_0_2_0;

public class Dependency_0_2_0(IDependency_0_2_0_0 dep20, IDependency_0_2_0_1 dep21, IDependency_0_2_0_2 dep22, IDependency_0_2_0_3 dep23): IDependency_0_2_0;

public interface IDependency_0_2_0_0;

public class Dependency_0_2_0_0(): IDependency_0_2_0_0;

public interface IDependency_0_2_0_1;

public class Dependency_0_2_0_1(): IDependency_0_2_0_1;

public interface IDependency_0_2_0_2;

public class Dependency_0_2_0_2(): IDependency_0_2_0_2;

public interface IDependency_0_2_0_3;

public class Dependency_0_2_0_3(): IDependency_0_2_0_3;

public interface IDependency_0_2_1;

public class Dependency_0_2_1(IDependency_0_2_1_0 dep20, IDependency_0_2_1_1 dep21, IDependency_0_2_1_2 dep22, IDependency_0_2_1_3 dep23): IDependency_0_2_1;

public interface IDependency_0_2_1_0;

public class Dependency_0_2_1_0(): IDependency_0_2_1_0;

public interface IDependency_0_2_1_1;

public class Dependency_0_2_1_1(): IDependency_0_2_1_1;

public interface IDependency_0_2_1_2;

public class Dependency_0_2_1_2(): IDependency_0_2_1_2;

public interface IDependency_0_2_1_3;

public class Dependency_0_2_1_3(): IDependency_0_2_1_3;

public interface IDependency_0_2_2;

public class Dependency_0_2_2(IDependency_0_2_2_0 dep20, IDependency_0_2_2_1 dep21, IDependency_0_2_2_2 dep22, IDependency_0_2_2_3 dep23): IDependency_0_2_2;

public interface IDependency_0_2_2_0;

public class Dependency_0_2_2_0(): IDependency_0_2_2_0;

public interface IDependency_0_2_2_1;

public class Dependency_0_2_2_1(): IDependency_0_2_2_1;

public interface IDependency_0_2_2_2;

public class Dependency_0_2_2_2(): IDependency_0_2_2_2;

public interface IDependency_0_2_2_3;

public class Dependency_0_2_2_3(): IDependency_0_2_2_3;

public interface IDependency_0_2_3;

public class Dependency_0_2_3(IDependency_0_2_3_0 dep20, IDependency_0_2_3_1 dep21, IDependency_0_2_3_2 dep22, IDependency_0_2_3_3 dep23): IDependency_0_2_3;

public interface IDependency_0_2_3_0;

public class Dependency_0_2_3_0(): IDependency_0_2_3_0;

public interface IDependency_0_2_3_1;

public class Dependency_0_2_3_1(): IDependency_0_2_3_1;

public interface IDependency_0_2_3_2;

public class Dependency_0_2_3_2(): IDependency_0_2_3_2;

public interface IDependency_0_2_3_3;

public class Dependency_0_2_3_3(): IDependency_0_2_3_3;

public interface IDependency_0_3;

public class Dependency_0_3(IDependency_0_3_0 dep10, IDependency_0_3_1 dep11, IDependency_0_3_2 dep12, IDependency_0_3_3 dep13): IDependency_0_3;

public interface IDependency_0_3_0;

public class Dependency_0_3_0(IDependency_0_3_0_0 dep20, IDependency_0_3_0_1 dep21, IDependency_0_3_0_2 dep22, IDependency_0_3_0_3 dep23): IDependency_0_3_0;

public interface IDependency_0_3_0_0;

public class Dependency_0_3_0_0(): IDependency_0_3_0_0;

public interface IDependency_0_3_0_1;

public class Dependency_0_3_0_1(): IDependency_0_3_0_1;

public interface IDependency_0_3_0_2;

public class Dependency_0_3_0_2(): IDependency_0_3_0_2;

public interface IDependency_0_3_0_3;

public class Dependency_0_3_0_3(): IDependency_0_3_0_3;

public interface IDependency_0_3_1;

public class Dependency_0_3_1(IDependency_0_3_1_0 dep20, IDependency_0_3_1_1 dep21, IDependency_0_3_1_2 dep22, IDependency_0_3_1_3 dep23): IDependency_0_3_1;

public interface IDependency_0_3_1_0;

public class Dependency_0_3_1_0(): IDependency_0_3_1_0;

public interface IDependency_0_3_1_1;

public class Dependency_0_3_1_1(): IDependency_0_3_1_1;

public interface IDependency_0_3_1_2;

public class Dependency_0_3_1_2(): IDependency_0_3_1_2;

public interface IDependency_0_3_1_3;

public class Dependency_0_3_1_3(): IDependency_0_3_1_3;

public interface IDependency_0_3_2;

public class Dependency_0_3_2(IDependency_0_3_2_0 dep20, IDependency_0_3_2_1 dep21, IDependency_0_3_2_2 dep22, IDependency_0_3_2_3 dep23): IDependency_0_3_2;

public interface IDependency_0_3_2_0;

public class Dependency_0_3_2_0(): IDependency_0_3_2_0;

public interface IDependency_0_3_2_1;

public class Dependency_0_3_2_1(): IDependency_0_3_2_1;

public interface IDependency_0_3_2_2;

public class Dependency_0_3_2_2(): IDependency_0_3_2_2;

public interface IDependency_0_3_2_3;

public class Dependency_0_3_2_3(): IDependency_0_3_2_3;

public interface IDependency_0_3_3;

public class Dependency_0_3_3(IDependency_0_3_3_0 dep20, IDependency_0_3_3_1 dep21, IDependency_0_3_3_2 dep22, IDependency_0_3_3_3 dep23): IDependency_0_3_3;

public interface IDependency_0_3_3_0;

public class Dependency_0_3_3_0(): IDependency_0_3_3_0;

public interface IDependency_0_3_3_1;

public class Dependency_0_3_3_1(): IDependency_0_3_3_1;

public interface IDependency_0_3_3_2;

public class Dependency_0_3_3_2(): IDependency_0_3_3_2;

public interface IDependency_0_3_3_3;

public class Dependency_0_3_3_3(): IDependency_0_3_3_3;

public interface IDependency_1;

public class Dependency_1(IDependency_1_0 dep00, IDependency_1_1 dep01, IDependency_1_2 dep02, IDependency_1_3 dep03): IDependency_1;

public interface IDependency_1_0;

public class Dependency_1_0(IDependency_1_0_0 dep10, IDependency_1_0_1 dep11, IDependency_1_0_2 dep12, IDependency_1_0_3 dep13): IDependency_1_0;

public interface IDependency_1_0_0;

public class Dependency_1_0_0(IDependency_1_0_0_0 dep20, IDependency_1_0_0_1 dep21, IDependency_1_0_0_2 dep22, IDependency_1_0_0_3 dep23): IDependency_1_0_0;

public interface IDependency_1_0_0_0;

public class Dependency_1_0_0_0(): IDependency_1_0_0_0;

public interface IDependency_1_0_0_1;

public class Dependency_1_0_0_1(): IDependency_1_0_0_1;

public interface IDependency_1_0_0_2;

public class Dependency_1_0_0_2(): IDependency_1_0_0_2;

public interface IDependency_1_0_0_3;

public class Dependency_1_0_0_3(): IDependency_1_0_0_3;

public interface IDependency_1_0_1;

public class Dependency_1_0_1(IDependency_1_0_1_0 dep20, IDependency_1_0_1_1 dep21, IDependency_1_0_1_2 dep22, IDependency_1_0_1_3 dep23): IDependency_1_0_1;

public interface IDependency_1_0_1_0;

public class Dependency_1_0_1_0(): IDependency_1_0_1_0;

public interface IDependency_1_0_1_1;

public class Dependency_1_0_1_1(): IDependency_1_0_1_1;

public interface IDependency_1_0_1_2;

public class Dependency_1_0_1_2(): IDependency_1_0_1_2;

public interface IDependency_1_0_1_3;

public class Dependency_1_0_1_3(): IDependency_1_0_1_3;

public interface IDependency_1_0_2;

public class Dependency_1_0_2(IDependency_1_0_2_0 dep20, IDependency_1_0_2_1 dep21, IDependency_1_0_2_2 dep22, IDependency_1_0_2_3 dep23): IDependency_1_0_2;

public interface IDependency_1_0_2_0;

public class Dependency_1_0_2_0(): IDependency_1_0_2_0;

public interface IDependency_1_0_2_1;

public class Dependency_1_0_2_1(): IDependency_1_0_2_1;

public interface IDependency_1_0_2_2;

public class Dependency_1_0_2_2(): IDependency_1_0_2_2;

public interface IDependency_1_0_2_3;

public class Dependency_1_0_2_3(): IDependency_1_0_2_3;

public interface IDependency_1_0_3;

public class Dependency_1_0_3(IDependency_1_0_3_0 dep20, IDependency_1_0_3_1 dep21, IDependency_1_0_3_2 dep22, IDependency_1_0_3_3 dep23): IDependency_1_0_3;

public interface IDependency_1_0_3_0;

public class Dependency_1_0_3_0(): IDependency_1_0_3_0;

public interface IDependency_1_0_3_1;

public class Dependency_1_0_3_1(): IDependency_1_0_3_1;

public interface IDependency_1_0_3_2;

public class Dependency_1_0_3_2(): IDependency_1_0_3_2;

public interface IDependency_1_0_3_3;

public class Dependency_1_0_3_3(): IDependency_1_0_3_3;

public interface IDependency_1_1;

public class Dependency_1_1(IDependency_1_1_0 dep10, IDependency_1_1_1 dep11, IDependency_1_1_2 dep12, IDependency_1_1_3 dep13): IDependency_1_1;

public interface IDependency_1_1_0;

public class Dependency_1_1_0(IDependency_1_1_0_0 dep20, IDependency_1_1_0_1 dep21, IDependency_1_1_0_2 dep22, IDependency_1_1_0_3 dep23): IDependency_1_1_0;

public interface IDependency_1_1_0_0;

public class Dependency_1_1_0_0(): IDependency_1_1_0_0;

public interface IDependency_1_1_0_1;

public class Dependency_1_1_0_1(): IDependency_1_1_0_1;

public interface IDependency_1_1_0_2;

public class Dependency_1_1_0_2(): IDependency_1_1_0_2;

public interface IDependency_1_1_0_3;

public class Dependency_1_1_0_3(): IDependency_1_1_0_3;

public interface IDependency_1_1_1;

public class Dependency_1_1_1(IDependency_1_1_1_0 dep20, IDependency_1_1_1_1 dep21, IDependency_1_1_1_2 dep22, IDependency_1_1_1_3 dep23): IDependency_1_1_1;

public interface IDependency_1_1_1_0;

public class Dependency_1_1_1_0(): IDependency_1_1_1_0;

public interface IDependency_1_1_1_1;

public class Dependency_1_1_1_1(): IDependency_1_1_1_1;

public interface IDependency_1_1_1_2;

public class Dependency_1_1_1_2(): IDependency_1_1_1_2;

public interface IDependency_1_1_1_3;

public class Dependency_1_1_1_3(): IDependency_1_1_1_3;

public interface IDependency_1_1_2;

public class Dependency_1_1_2(IDependency_1_1_2_0 dep20, IDependency_1_1_2_1 dep21, IDependency_1_1_2_2 dep22, IDependency_1_1_2_3 dep23): IDependency_1_1_2;

public interface IDependency_1_1_2_0;

public class Dependency_1_1_2_0(): IDependency_1_1_2_0;

public interface IDependency_1_1_2_1;

public class Dependency_1_1_2_1(): IDependency_1_1_2_1;

public interface IDependency_1_1_2_2;

public class Dependency_1_1_2_2(): IDependency_1_1_2_2;

public interface IDependency_1_1_2_3;

public class Dependency_1_1_2_3(): IDependency_1_1_2_3;

public interface IDependency_1_1_3;

public class Dependency_1_1_3(IDependency_1_1_3_0 dep20, IDependency_1_1_3_1 dep21, IDependency_1_1_3_2 dep22, IDependency_1_1_3_3 dep23): IDependency_1_1_3;

public interface IDependency_1_1_3_0;

public class Dependency_1_1_3_0(): IDependency_1_1_3_0;

public interface IDependency_1_1_3_1;

public class Dependency_1_1_3_1(): IDependency_1_1_3_1;

public interface IDependency_1_1_3_2;

public class Dependency_1_1_3_2(): IDependency_1_1_3_2;

public interface IDependency_1_1_3_3;

public class Dependency_1_1_3_3(): IDependency_1_1_3_3;

public interface IDependency_1_2;

public class Dependency_1_2(IDependency_1_2_0 dep10, IDependency_1_2_1 dep11, IDependency_1_2_2 dep12, IDependency_1_2_3 dep13): IDependency_1_2;

public interface IDependency_1_2_0;

public class Dependency_1_2_0(IDependency_1_2_0_0 dep20, IDependency_1_2_0_1 dep21, IDependency_1_2_0_2 dep22, IDependency_1_2_0_3 dep23): IDependency_1_2_0;

public interface IDependency_1_2_0_0;

public class Dependency_1_2_0_0(): IDependency_1_2_0_0;

public interface IDependency_1_2_0_1;

public class Dependency_1_2_0_1(): IDependency_1_2_0_1;

public interface IDependency_1_2_0_2;

public class Dependency_1_2_0_2(): IDependency_1_2_0_2;

public interface IDependency_1_2_0_3;

public class Dependency_1_2_0_3(): IDependency_1_2_0_3;

public interface IDependency_1_2_1;

public class Dependency_1_2_1(IDependency_1_2_1_0 dep20, IDependency_1_2_1_1 dep21, IDependency_1_2_1_2 dep22, IDependency_1_2_1_3 dep23): IDependency_1_2_1;

public interface IDependency_1_2_1_0;

public class Dependency_1_2_1_0(): IDependency_1_2_1_0;

public interface IDependency_1_2_1_1;

public class Dependency_1_2_1_1(): IDependency_1_2_1_1;

public interface IDependency_1_2_1_2;

public class Dependency_1_2_1_2(): IDependency_1_2_1_2;

public interface IDependency_1_2_1_3;

public class Dependency_1_2_1_3(): IDependency_1_2_1_3;

public interface IDependency_1_2_2;

public class Dependency_1_2_2(IDependency_1_2_2_0 dep20, IDependency_1_2_2_1 dep21, IDependency_1_2_2_2 dep22, IDependency_1_2_2_3 dep23): IDependency_1_2_2;

public interface IDependency_1_2_2_0;

public class Dependency_1_2_2_0(): IDependency_1_2_2_0;

public interface IDependency_1_2_2_1;

public class Dependency_1_2_2_1(): IDependency_1_2_2_1;

public interface IDependency_1_2_2_2;

public class Dependency_1_2_2_2(): IDependency_1_2_2_2;

public interface IDependency_1_2_2_3;

public class Dependency_1_2_2_3(): IDependency_1_2_2_3;

public interface IDependency_1_2_3;

public class Dependency_1_2_3(IDependency_1_2_3_0 dep20, IDependency_1_2_3_1 dep21, IDependency_1_2_3_2 dep22, IDependency_1_2_3_3 dep23): IDependency_1_2_3;

public interface IDependency_1_2_3_0;

public class Dependency_1_2_3_0(): IDependency_1_2_3_0;

public interface IDependency_1_2_3_1;

public class Dependency_1_2_3_1(): IDependency_1_2_3_1;

public interface IDependency_1_2_3_2;

public class Dependency_1_2_3_2(): IDependency_1_2_3_2;

public interface IDependency_1_2_3_3;

public class Dependency_1_2_3_3(): IDependency_1_2_3_3;

public interface IDependency_1_3;

public class Dependency_1_3(IDependency_1_3_0 dep10, IDependency_1_3_1 dep11, IDependency_1_3_2 dep12, IDependency_1_3_3 dep13): IDependency_1_3;

public interface IDependency_1_3_0;

public class Dependency_1_3_0(IDependency_1_3_0_0 dep20, IDependency_1_3_0_1 dep21, IDependency_1_3_0_2 dep22, IDependency_1_3_0_3 dep23): IDependency_1_3_0;

public interface IDependency_1_3_0_0;

public class Dependency_1_3_0_0(): IDependency_1_3_0_0;

public interface IDependency_1_3_0_1;

public class Dependency_1_3_0_1(): IDependency_1_3_0_1;

public interface IDependency_1_3_0_2;

public class Dependency_1_3_0_2(): IDependency_1_3_0_2;

public interface IDependency_1_3_0_3;

public class Dependency_1_3_0_3(): IDependency_1_3_0_3;

public interface IDependency_1_3_1;

public class Dependency_1_3_1(IDependency_1_3_1_0 dep20, IDependency_1_3_1_1 dep21, IDependency_1_3_1_2 dep22, IDependency_1_3_1_3 dep23): IDependency_1_3_1;

public interface IDependency_1_3_1_0;

public class Dependency_1_3_1_0(): IDependency_1_3_1_0;

public interface IDependency_1_3_1_1;

public class Dependency_1_3_1_1(): IDependency_1_3_1_1;

public interface IDependency_1_3_1_2;

public class Dependency_1_3_1_2(): IDependency_1_3_1_2;

public interface IDependency_1_3_1_3;

public class Dependency_1_3_1_3(): IDependency_1_3_1_3;

public interface IDependency_1_3_2;

public class Dependency_1_3_2(IDependency_1_3_2_0 dep20, IDependency_1_3_2_1 dep21, IDependency_1_3_2_2 dep22, IDependency_1_3_2_3 dep23): IDependency_1_3_2;

public interface IDependency_1_3_2_0;

public class Dependency_1_3_2_0(): IDependency_1_3_2_0;

public interface IDependency_1_3_2_1;

public class Dependency_1_3_2_1(): IDependency_1_3_2_1;

public interface IDependency_1_3_2_2;

public class Dependency_1_3_2_2(): IDependency_1_3_2_2;

public interface IDependency_1_3_2_3;

public class Dependency_1_3_2_3(): IDependency_1_3_2_3;

public interface IDependency_1_3_3;

public class Dependency_1_3_3(IDependency_1_3_3_0 dep20, IDependency_1_3_3_1 dep21, IDependency_1_3_3_2 dep22, IDependency_1_3_3_3 dep23): IDependency_1_3_3;

public interface IDependency_1_3_3_0;

public class Dependency_1_3_3_0(): IDependency_1_3_3_0;

public interface IDependency_1_3_3_1;

public class Dependency_1_3_3_1(): IDependency_1_3_3_1;

public interface IDependency_1_3_3_2;

public class Dependency_1_3_3_2(): IDependency_1_3_3_2;

public interface IDependency_1_3_3_3;

public class Dependency_1_3_3_3(): IDependency_1_3_3_3;

public interface IDependency_2;

public class Dependency_2(IDependency_2_0 dep00, IDependency_2_1 dep01, IDependency_2_2 dep02, IDependency_2_3 dep03): IDependency_2;

public interface IDependency_2_0;

public class Dependency_2_0(IDependency_2_0_0 dep10, IDependency_2_0_1 dep11, IDependency_2_0_2 dep12, IDependency_2_0_3 dep13): IDependency_2_0;

public interface IDependency_2_0_0;

public class Dependency_2_0_0(IDependency_2_0_0_0 dep20, IDependency_2_0_0_1 dep21, IDependency_2_0_0_2 dep22, IDependency_2_0_0_3 dep23): IDependency_2_0_0;

public interface IDependency_2_0_0_0;

public class Dependency_2_0_0_0(): IDependency_2_0_0_0;

public interface IDependency_2_0_0_1;

public class Dependency_2_0_0_1(): IDependency_2_0_0_1;

public interface IDependency_2_0_0_2;

public class Dependency_2_0_0_2(): IDependency_2_0_0_2;

public interface IDependency_2_0_0_3;

public class Dependency_2_0_0_3(): IDependency_2_0_0_3;

public interface IDependency_2_0_1;

public class Dependency_2_0_1(IDependency_2_0_1_0 dep20, IDependency_2_0_1_1 dep21, IDependency_2_0_1_2 dep22, IDependency_2_0_1_3 dep23): IDependency_2_0_1;

public interface IDependency_2_0_1_0;

public class Dependency_2_0_1_0(): IDependency_2_0_1_0;

public interface IDependency_2_0_1_1;

public class Dependency_2_0_1_1(): IDependency_2_0_1_1;

public interface IDependency_2_0_1_2;

public class Dependency_2_0_1_2(): IDependency_2_0_1_2;

public interface IDependency_2_0_1_3;

public class Dependency_2_0_1_3(): IDependency_2_0_1_3;

public interface IDependency_2_0_2;

public class Dependency_2_0_2(IDependency_2_0_2_0 dep20, IDependency_2_0_2_1 dep21, IDependency_2_0_2_2 dep22, IDependency_2_0_2_3 dep23): IDependency_2_0_2;

public interface IDependency_2_0_2_0;

public class Dependency_2_0_2_0(): IDependency_2_0_2_0;

public interface IDependency_2_0_2_1;

public class Dependency_2_0_2_1(): IDependency_2_0_2_1;

public interface IDependency_2_0_2_2;

public class Dependency_2_0_2_2(): IDependency_2_0_2_2;

public interface IDependency_2_0_2_3;

public class Dependency_2_0_2_3(): IDependency_2_0_2_3;

public interface IDependency_2_0_3;

public class Dependency_2_0_3(IDependency_2_0_3_0 dep20, IDependency_2_0_3_1 dep21, IDependency_2_0_3_2 dep22, IDependency_2_0_3_3 dep23): IDependency_2_0_3;

public interface IDependency_2_0_3_0;

public class Dependency_2_0_3_0(): IDependency_2_0_3_0;

public interface IDependency_2_0_3_1;

public class Dependency_2_0_3_1(): IDependency_2_0_3_1;

public interface IDependency_2_0_3_2;

public class Dependency_2_0_3_2(): IDependency_2_0_3_2;

public interface IDependency_2_0_3_3;

public class Dependency_2_0_3_3(): IDependency_2_0_3_3;

public interface IDependency_2_1;

public class Dependency_2_1(IDependency_2_1_0 dep10, IDependency_2_1_1 dep11, IDependency_2_1_2 dep12, IDependency_2_1_3 dep13): IDependency_2_1;

public interface IDependency_2_1_0;

public class Dependency_2_1_0(IDependency_2_1_0_0 dep20, IDependency_2_1_0_1 dep21, IDependency_2_1_0_2 dep22, IDependency_2_1_0_3 dep23): IDependency_2_1_0;

public interface IDependency_2_1_0_0;

public class Dependency_2_1_0_0(): IDependency_2_1_0_0;

public interface IDependency_2_1_0_1;

public class Dependency_2_1_0_1(): IDependency_2_1_0_1;

public interface IDependency_2_1_0_2;

public class Dependency_2_1_0_2(): IDependency_2_1_0_2;

public interface IDependency_2_1_0_3;

public class Dependency_2_1_0_3(): IDependency_2_1_0_3;

public interface IDependency_2_1_1;

public class Dependency_2_1_1(IDependency_2_1_1_0 dep20, IDependency_2_1_1_1 dep21, IDependency_2_1_1_2 dep22, IDependency_2_1_1_3 dep23): IDependency_2_1_1;

public interface IDependency_2_1_1_0;

public class Dependency_2_1_1_0(): IDependency_2_1_1_0;

public interface IDependency_2_1_1_1;

public class Dependency_2_1_1_1(): IDependency_2_1_1_1;

public interface IDependency_2_1_1_2;

public class Dependency_2_1_1_2(): IDependency_2_1_1_2;

public interface IDependency_2_1_1_3;

public class Dependency_2_1_1_3(): IDependency_2_1_1_3;

public interface IDependency_2_1_2;

public class Dependency_2_1_2(IDependency_2_1_2_0 dep20, IDependency_2_1_2_1 dep21, IDependency_2_1_2_2 dep22, IDependency_2_1_2_3 dep23): IDependency_2_1_2;

public interface IDependency_2_1_2_0;

public class Dependency_2_1_2_0(): IDependency_2_1_2_0;

public interface IDependency_2_1_2_1;

public class Dependency_2_1_2_1(): IDependency_2_1_2_1;

public interface IDependency_2_1_2_2;

public class Dependency_2_1_2_2(): IDependency_2_1_2_2;

public interface IDependency_2_1_2_3;

public class Dependency_2_1_2_3(): IDependency_2_1_2_3;

public interface IDependency_2_1_3;

public class Dependency_2_1_3(IDependency_2_1_3_0 dep20, IDependency_2_1_3_1 dep21, IDependency_2_1_3_2 dep22, IDependency_2_1_3_3 dep23): IDependency_2_1_3;

public interface IDependency_2_1_3_0;

public class Dependency_2_1_3_0(): IDependency_2_1_3_0;

public interface IDependency_2_1_3_1;

public class Dependency_2_1_3_1(): IDependency_2_1_3_1;

public interface IDependency_2_1_3_2;

public class Dependency_2_1_3_2(): IDependency_2_1_3_2;

public interface IDependency_2_1_3_3;

public class Dependency_2_1_3_3(): IDependency_2_1_3_3;

public interface IDependency_2_2;

public class Dependency_2_2(IDependency_2_2_0 dep10, IDependency_2_2_1 dep11, IDependency_2_2_2 dep12, IDependency_2_2_3 dep13): IDependency_2_2;

public interface IDependency_2_2_0;

public class Dependency_2_2_0(IDependency_2_2_0_0 dep20, IDependency_2_2_0_1 dep21, IDependency_2_2_0_2 dep22, IDependency_2_2_0_3 dep23): IDependency_2_2_0;

public interface IDependency_2_2_0_0;

public class Dependency_2_2_0_0(): IDependency_2_2_0_0;

public interface IDependency_2_2_0_1;

public class Dependency_2_2_0_1(): IDependency_2_2_0_1;

public interface IDependency_2_2_0_2;

public class Dependency_2_2_0_2(): IDependency_2_2_0_2;

public interface IDependency_2_2_0_3;

public class Dependency_2_2_0_3(): IDependency_2_2_0_3;

public interface IDependency_2_2_1;

public class Dependency_2_2_1(IDependency_2_2_1_0 dep20, IDependency_2_2_1_1 dep21, IDependency_2_2_1_2 dep22, IDependency_2_2_1_3 dep23): IDependency_2_2_1;

public interface IDependency_2_2_1_0;

public class Dependency_2_2_1_0(): IDependency_2_2_1_0;

public interface IDependency_2_2_1_1;

public class Dependency_2_2_1_1(): IDependency_2_2_1_1;

public interface IDependency_2_2_1_2;

public class Dependency_2_2_1_2(): IDependency_2_2_1_2;

public interface IDependency_2_2_1_3;

public class Dependency_2_2_1_3(): IDependency_2_2_1_3;

public interface IDependency_2_2_2;

public class Dependency_2_2_2(IDependency_2_2_2_0 dep20, IDependency_2_2_2_1 dep21, IDependency_2_2_2_2 dep22, IDependency_2_2_2_3 dep23): IDependency_2_2_2;

public interface IDependency_2_2_2_0;

public class Dependency_2_2_2_0(): IDependency_2_2_2_0;

public interface IDependency_2_2_2_1;

public class Dependency_2_2_2_1(): IDependency_2_2_2_1;

public interface IDependency_2_2_2_2;

public class Dependency_2_2_2_2(): IDependency_2_2_2_2;

public interface IDependency_2_2_2_3;

public class Dependency_2_2_2_3(): IDependency_2_2_2_3;

public interface IDependency_2_2_3;

public class Dependency_2_2_3(IDependency_2_2_3_0 dep20, IDependency_2_2_3_1 dep21, IDependency_2_2_3_2 dep22, IDependency_2_2_3_3 dep23): IDependency_2_2_3;

public interface IDependency_2_2_3_0;

public class Dependency_2_2_3_0(): IDependency_2_2_3_0;

public interface IDependency_2_2_3_1;

public class Dependency_2_2_3_1(): IDependency_2_2_3_1;

public interface IDependency_2_2_3_2;

public class Dependency_2_2_3_2(): IDependency_2_2_3_2;

public interface IDependency_2_2_3_3;

public class Dependency_2_2_3_3(): IDependency_2_2_3_3;

public interface IDependency_2_3;

public class Dependency_2_3(IDependency_2_3_0 dep10, IDependency_2_3_1 dep11, IDependency_2_3_2 dep12, IDependency_2_3_3 dep13): IDependency_2_3;

public interface IDependency_2_3_0;

public class Dependency_2_3_0(IDependency_2_3_0_0 dep20, IDependency_2_3_0_1 dep21, IDependency_2_3_0_2 dep22, IDependency_2_3_0_3 dep23): IDependency_2_3_0;

public interface IDependency_2_3_0_0;

public class Dependency_2_3_0_0(): IDependency_2_3_0_0;

public interface IDependency_2_3_0_1;

public class Dependency_2_3_0_1(): IDependency_2_3_0_1;

public interface IDependency_2_3_0_2;

public class Dependency_2_3_0_2(): IDependency_2_3_0_2;

public interface IDependency_2_3_0_3;

public class Dependency_2_3_0_3(): IDependency_2_3_0_3;

public interface IDependency_2_3_1;

public class Dependency_2_3_1(IDependency_2_3_1_0 dep20, IDependency_2_3_1_1 dep21, IDependency_2_3_1_2 dep22, IDependency_2_3_1_3 dep23): IDependency_2_3_1;

public interface IDependency_2_3_1_0;

public class Dependency_2_3_1_0(): IDependency_2_3_1_0;

public interface IDependency_2_3_1_1;

public class Dependency_2_3_1_1(): IDependency_2_3_1_1;

public interface IDependency_2_3_1_2;

public class Dependency_2_3_1_2(): IDependency_2_3_1_2;

public interface IDependency_2_3_1_3;

public class Dependency_2_3_1_3(): IDependency_2_3_1_3;

public interface IDependency_2_3_2;

public class Dependency_2_3_2(IDependency_2_3_2_0 dep20, IDependency_2_3_2_1 dep21, IDependency_2_3_2_2 dep22, IDependency_2_3_2_3 dep23): IDependency_2_3_2;

public interface IDependency_2_3_2_0;

public class Dependency_2_3_2_0(): IDependency_2_3_2_0;

public interface IDependency_2_3_2_1;

public class Dependency_2_3_2_1(): IDependency_2_3_2_1;

public interface IDependency_2_3_2_2;

public class Dependency_2_3_2_2(): IDependency_2_3_2_2;

public interface IDependency_2_3_2_3;

public class Dependency_2_3_2_3(): IDependency_2_3_2_3;

public interface IDependency_2_3_3;

public class Dependency_2_3_3(IDependency_2_3_3_0 dep20, IDependency_2_3_3_1 dep21, IDependency_2_3_3_2 dep22, IDependency_2_3_3_3 dep23): IDependency_2_3_3;

public interface IDependency_2_3_3_0;

public class Dependency_2_3_3_0(): IDependency_2_3_3_0;

public interface IDependency_2_3_3_1;

public class Dependency_2_3_3_1(): IDependency_2_3_3_1;

public interface IDependency_2_3_3_2;

public class Dependency_2_3_3_2(): IDependency_2_3_3_2;

public interface IDependency_2_3_3_3;

public class Dependency_2_3_3_3(): IDependency_2_3_3_3;

public interface IDependency_3;

public class Dependency_3(IDependency_3_0 dep00, IDependency_3_1 dep01, IDependency_3_2 dep02, IDependency_3_3 dep03): IDependency_3;

public interface IDependency_3_0;

public class Dependency_3_0(IDependency_3_0_0 dep10, IDependency_3_0_1 dep11, IDependency_3_0_2 dep12, IDependency_3_0_3 dep13): IDependency_3_0;

public interface IDependency_3_0_0;

public class Dependency_3_0_0(IDependency_3_0_0_0 dep20, IDependency_3_0_0_1 dep21, IDependency_3_0_0_2 dep22, IDependency_3_0_0_3 dep23): IDependency_3_0_0;

public interface IDependency_3_0_0_0;

public class Dependency_3_0_0_0(): IDependency_3_0_0_0;

public interface IDependency_3_0_0_1;

public class Dependency_3_0_0_1(): IDependency_3_0_0_1;

public interface IDependency_3_0_0_2;

public class Dependency_3_0_0_2(): IDependency_3_0_0_2;

public interface IDependency_3_0_0_3;

public class Dependency_3_0_0_3(): IDependency_3_0_0_3;

public interface IDependency_3_0_1;

public class Dependency_3_0_1(IDependency_3_0_1_0 dep20, IDependency_3_0_1_1 dep21, IDependency_3_0_1_2 dep22, IDependency_3_0_1_3 dep23): IDependency_3_0_1;

public interface IDependency_3_0_1_0;

public class Dependency_3_0_1_0(): IDependency_3_0_1_0;

public interface IDependency_3_0_1_1;

public class Dependency_3_0_1_1(): IDependency_3_0_1_1;

public interface IDependency_3_0_1_2;

public class Dependency_3_0_1_2(): IDependency_3_0_1_2;

public interface IDependency_3_0_1_3;

public class Dependency_3_0_1_3(): IDependency_3_0_1_3;

public interface IDependency_3_0_2;

public class Dependency_3_0_2(IDependency_3_0_2_0 dep20, IDependency_3_0_2_1 dep21, IDependency_3_0_2_2 dep22, IDependency_3_0_2_3 dep23): IDependency_3_0_2;

public interface IDependency_3_0_2_0;

public class Dependency_3_0_2_0(): IDependency_3_0_2_0;

public interface IDependency_3_0_2_1;

public class Dependency_3_0_2_1(): IDependency_3_0_2_1;

public interface IDependency_3_0_2_2;

public class Dependency_3_0_2_2(): IDependency_3_0_2_2;

public interface IDependency_3_0_2_3;

public class Dependency_3_0_2_3(): IDependency_3_0_2_3;

public interface IDependency_3_0_3;

public class Dependency_3_0_3(IDependency_3_0_3_0 dep20, IDependency_3_0_3_1 dep21, IDependency_3_0_3_2 dep22, IDependency_3_0_3_3 dep23): IDependency_3_0_3;

public interface IDependency_3_0_3_0;

public class Dependency_3_0_3_0(): IDependency_3_0_3_0;

public interface IDependency_3_0_3_1;

public class Dependency_3_0_3_1(): IDependency_3_0_3_1;

public interface IDependency_3_0_3_2;

public class Dependency_3_0_3_2(): IDependency_3_0_3_2;

public interface IDependency_3_0_3_3;

public class Dependency_3_0_3_3(): IDependency_3_0_3_3;

public interface IDependency_3_1;

public class Dependency_3_1(IDependency_3_1_0 dep10, IDependency_3_1_1 dep11, IDependency_3_1_2 dep12, IDependency_3_1_3 dep13): IDependency_3_1;

public interface IDependency_3_1_0;

public class Dependency_3_1_0(IDependency_3_1_0_0 dep20, IDependency_3_1_0_1 dep21, IDependency_3_1_0_2 dep22, IDependency_3_1_0_3 dep23): IDependency_3_1_0;

public interface IDependency_3_1_0_0;

public class Dependency_3_1_0_0(): IDependency_3_1_0_0;

public interface IDependency_3_1_0_1;

public class Dependency_3_1_0_1(): IDependency_3_1_0_1;

public interface IDependency_3_1_0_2;

public class Dependency_3_1_0_2(): IDependency_3_1_0_2;

public interface IDependency_3_1_0_3;

public class Dependency_3_1_0_3(): IDependency_3_1_0_3;

public interface IDependency_3_1_1;

public class Dependency_3_1_1(IDependency_3_1_1_0 dep20, IDependency_3_1_1_1 dep21, IDependency_3_1_1_2 dep22, IDependency_3_1_1_3 dep23): IDependency_3_1_1;

public interface IDependency_3_1_1_0;

public class Dependency_3_1_1_0(): IDependency_3_1_1_0;

public interface IDependency_3_1_1_1;

public class Dependency_3_1_1_1(): IDependency_3_1_1_1;

public interface IDependency_3_1_1_2;

public class Dependency_3_1_1_2(): IDependency_3_1_1_2;

public interface IDependency_3_1_1_3;

public class Dependency_3_1_1_3(): IDependency_3_1_1_3;

public interface IDependency_3_1_2;

public class Dependency_3_1_2(IDependency_3_1_2_0 dep20, IDependency_3_1_2_1 dep21, IDependency_3_1_2_2 dep22, IDependency_3_1_2_3 dep23): IDependency_3_1_2;

public interface IDependency_3_1_2_0;

public class Dependency_3_1_2_0(): IDependency_3_1_2_0;

public interface IDependency_3_1_2_1;

public class Dependency_3_1_2_1(): IDependency_3_1_2_1;

public interface IDependency_3_1_2_2;

public class Dependency_3_1_2_2(): IDependency_3_1_2_2;

public interface IDependency_3_1_2_3;

public class Dependency_3_1_2_3(): IDependency_3_1_2_3;

public interface IDependency_3_1_3;

public class Dependency_3_1_3(IDependency_3_1_3_0 dep20, IDependency_3_1_3_1 dep21, IDependency_3_1_3_2 dep22, IDependency_3_1_3_3 dep23): IDependency_3_1_3;

public interface IDependency_3_1_3_0;

public class Dependency_3_1_3_0(): IDependency_3_1_3_0;

public interface IDependency_3_1_3_1;

public class Dependency_3_1_3_1(): IDependency_3_1_3_1;

public interface IDependency_3_1_3_2;

public class Dependency_3_1_3_2(): IDependency_3_1_3_2;

public interface IDependency_3_1_3_3;

public class Dependency_3_1_3_3(): IDependency_3_1_3_3;

public interface IDependency_3_2;

public class Dependency_3_2(IDependency_3_2_0 dep10, IDependency_3_2_1 dep11, IDependency_3_2_2 dep12, IDependency_3_2_3 dep13): IDependency_3_2;

public interface IDependency_3_2_0;

public class Dependency_3_2_0(IDependency_3_2_0_0 dep20, IDependency_3_2_0_1 dep21, IDependency_3_2_0_2 dep22, IDependency_3_2_0_3 dep23): IDependency_3_2_0;

public interface IDependency_3_2_0_0;

public class Dependency_3_2_0_0(): IDependency_3_2_0_0;

public interface IDependency_3_2_0_1;

public class Dependency_3_2_0_1(): IDependency_3_2_0_1;

public interface IDependency_3_2_0_2;

public class Dependency_3_2_0_2(): IDependency_3_2_0_2;

public interface IDependency_3_2_0_3;

public class Dependency_3_2_0_3(): IDependency_3_2_0_3;

public interface IDependency_3_2_1;

public class Dependency_3_2_1(IDependency_3_2_1_0 dep20, IDependency_3_2_1_1 dep21, IDependency_3_2_1_2 dep22, IDependency_3_2_1_3 dep23): IDependency_3_2_1;

public interface IDependency_3_2_1_0;

public class Dependency_3_2_1_0(): IDependency_3_2_1_0;

public interface IDependency_3_2_1_1;

public class Dependency_3_2_1_1(): IDependency_3_2_1_1;

public interface IDependency_3_2_1_2;

public class Dependency_3_2_1_2(): IDependency_3_2_1_2;

public interface IDependency_3_2_1_3;

public class Dependency_3_2_1_3(): IDependency_3_2_1_3;

public interface IDependency_3_2_2;

public class Dependency_3_2_2(IDependency_3_2_2_0 dep20, IDependency_3_2_2_1 dep21, IDependency_3_2_2_2 dep22, IDependency_3_2_2_3 dep23): IDependency_3_2_2;

public interface IDependency_3_2_2_0;

public class Dependency_3_2_2_0(): IDependency_3_2_2_0;

public interface IDependency_3_2_2_1;

public class Dependency_3_2_2_1(): IDependency_3_2_2_1;

public interface IDependency_3_2_2_2;

public class Dependency_3_2_2_2(): IDependency_3_2_2_2;

public interface IDependency_3_2_2_3;

public class Dependency_3_2_2_3(): IDependency_3_2_2_3;

public interface IDependency_3_2_3;

public class Dependency_3_2_3(IDependency_3_2_3_0 dep20, IDependency_3_2_3_1 dep21, IDependency_3_2_3_2 dep22, IDependency_3_2_3_3 dep23): IDependency_3_2_3;

public interface IDependency_3_2_3_0;

public class Dependency_3_2_3_0(): IDependency_3_2_3_0;

public interface IDependency_3_2_3_1;

public class Dependency_3_2_3_1(): IDependency_3_2_3_1;

public interface IDependency_3_2_3_2;

public class Dependency_3_2_3_2(): IDependency_3_2_3_2;

public interface IDependency_3_2_3_3;

public class Dependency_3_2_3_3(): IDependency_3_2_3_3;

public interface IDependency_3_3;

public class Dependency_3_3(IDependency_3_3_0 dep10, IDependency_3_3_1 dep11, IDependency_3_3_2 dep12, IDependency_3_3_3 dep13): IDependency_3_3;

public interface IDependency_3_3_0;

public class Dependency_3_3_0(IDependency_3_3_0_0 dep20, IDependency_3_3_0_1 dep21, IDependency_3_3_0_2 dep22, IDependency_3_3_0_3 dep23): IDependency_3_3_0;

public interface IDependency_3_3_0_0;

public class Dependency_3_3_0_0(): IDependency_3_3_0_0;

public interface IDependency_3_3_0_1;

public class Dependency_3_3_0_1(): IDependency_3_3_0_1;

public interface IDependency_3_3_0_2;

public class Dependency_3_3_0_2(): IDependency_3_3_0_2;

public interface IDependency_3_3_0_3;

public class Dependency_3_3_0_3(): IDependency_3_3_0_3;

public interface IDependency_3_3_1;

public class Dependency_3_3_1(IDependency_3_3_1_0 dep20, IDependency_3_3_1_1 dep21, IDependency_3_3_1_2 dep22, IDependency_3_3_1_3 dep23): IDependency_3_3_1;

public interface IDependency_3_3_1_0;

public class Dependency_3_3_1_0(): IDependency_3_3_1_0;

public interface IDependency_3_3_1_1;

public class Dependency_3_3_1_1(): IDependency_3_3_1_1;

public interface IDependency_3_3_1_2;

public class Dependency_3_3_1_2(): IDependency_3_3_1_2;

public interface IDependency_3_3_1_3;

public class Dependency_3_3_1_3(): IDependency_3_3_1_3;

public interface IDependency_3_3_2;

public class Dependency_3_3_2(IDependency_3_3_2_0 dep20, IDependency_3_3_2_1 dep21, IDependency_3_3_2_2 dep22, IDependency_3_3_2_3 dep23): IDependency_3_3_2;

public interface IDependency_3_3_2_0;

public class Dependency_3_3_2_0(): IDependency_3_3_2_0;

public interface IDependency_3_3_2_1;

public class Dependency_3_3_2_1(): IDependency_3_3_2_1;

public interface IDependency_3_3_2_2;

public class Dependency_3_3_2_2(): IDependency_3_3_2_2;

public interface IDependency_3_3_2_3;

public class Dependency_3_3_2_3(): IDependency_3_3_2_3;

public interface IDependency_3_3_3;

public class Dependency_3_3_3(IDependency_3_3_3_0 dep20, IDependency_3_3_3_1 dep21, IDependency_3_3_3_2 dep22, IDependency_3_3_3_3 dep23): IDependency_3_3_3;

public interface IDependency_3_3_3_0;

public class Dependency_3_3_3_0(): IDependency_3_3_3_0;

public interface IDependency_3_3_3_1;

public class Dependency_3_3_3_1(): IDependency_3_3_3_1;

public interface IDependency_3_3_3_2;

public class Dependency_3_3_3_2(): IDependency_3_3_3_2;

public interface IDependency_3_3_3_3;

public class Dependency_3_3_3_3(): IDependency_3_3_3_3;



public static class Programm
{
	public static void Main()
	{
		var composition = new HugeComposition();
		var root = composition.Root;
	}
}
