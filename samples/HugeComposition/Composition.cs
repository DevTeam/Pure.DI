// ReSharper disable InconsistentNaming
// ReSharper disable EmptyConstructor
// ReSharper disable UnusedMember.Local
#pragma warning disable CS9113 // Parameter is unread.

namespace HugeComposition;

using Pure.DI;

// Total bindings: 121

public partial class Composition
{
	private void Setup() => DI.Setup()
		.Bind<IRoot>().As(Lifetime.PerBlock).To<Root>().Root<IRoot>("Root");

	private void Setup_0() => DI.Setup()
		.Bind<IDependency_0_0_0_0>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_0_0_0_0>(out var val20);
			return val20;
		}).Root<IDependency_0_0_0_0>();

	private void Setup_1() => DI.Setup()
		.Bind<IDependency_0_0_0_1>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_0_0_0_1>(out var val21);
			return val21;
		}).Root<IDependency_0_0_0_1>();

	private void Setup_2() => DI.Setup()
		.Bind<IDependency_0_0_0_2>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_0_0_0_2>(out var val22);
			return val22;
		}).Root<IDependency_0_0_0_2>();

	private void Setup_3() => DI.Setup()
		.Bind<IDependency_0_0_0>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_0_0_0>(out var val10);
			return val10;
		}).Root<IDependency_0_0_0>();

	private void Setup_4() => DI.Setup()
		.Bind<IDependency_0_0_1_0>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_0_0_1_0>(out var val20);
			return val20;
		}).Root<IDependency_0_0_1_0>();

	private void Setup_5() => DI.Setup()
		.Bind<IDependency_0_0_1_1>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_0_0_1_1>(out var val21);
			return val21;
		}).Root<IDependency_0_0_1_1>();

	private void Setup_6() => DI.Setup()
		.Bind<IDependency_0_0_1_2>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_0_0_1_2>(out var val22);
			return val22;
		}).Root<IDependency_0_0_1_2>();

	private void Setup_7() => DI.Setup()
		.Bind<IDependency_0_0_1>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_0_0_1>(out var val11);
			return val11;
		}).Root<IDependency_0_0_1>();

	private void Setup_8() => DI.Setup()
		.Bind<IDependency_0_0_2_0>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_0_0_2_0>(out var val20);
			return val20;
		}).Root<IDependency_0_0_2_0>();

	private void Setup_9() => DI.Setup()
		.Bind<IDependency_0_0_2_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_0_0_2_1>(out var val21);
			return val21;
		}).Root<IDependency_0_0_2_1>();

	private void Setup_10() => DI.Setup()
		.Bind<IDependency_0_0_2_2>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_0_0_2_2>(out var val22);
			return val22;
		}).Root<IDependency_0_0_2_2>();

	private void Setup_11() => DI.Setup()
		.Bind<IDependency_0_0_2>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_0_0_2>(out var val12);
			return val12;
		}).Root<IDependency_0_0_2>();

	private void Setup_12() => DI.Setup()
		.Bind<IDependency_0_0>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_0_0>(out var val00);
			return val00;
		}).Root<IDependency_0_0>();

	private void Setup_13() => DI.Setup()
		.Bind<IDependency_0_1_0_0>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_0_1_0_0>(out var val20);
			return val20;
		}).Root<IDependency_0_1_0_0>();

	private void Setup_14() => DI.Setup()
		.Bind<IDependency_0_1_0_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_0_1_0_1>(out var val21);
			return val21;
		}).Root<IDependency_0_1_0_1>();

	private void Setup_15() => DI.Setup()
		.Bind<IDependency_0_1_0_2>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_0_1_0_2>(out var val22);
			return val22;
		}).Root<IDependency_0_1_0_2>();

	private void Setup_16() => DI.Setup()
		.Bind<IDependency_0_1_0>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_0_1_0>(out var val10);
			return val10;
		}).Root<IDependency_0_1_0>();

	private void Setup_17() => DI.Setup()
		.Bind<IDependency_0_1_1_0>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_0_1_1_0>(out var val20);
			return val20;
		}).Root<IDependency_0_1_1_0>();

	private void Setup_18() => DI.Setup()
		.Bind<IDependency_0_1_1_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_0_1_1_1>(out var val21);
			return val21;
		}).Root<IDependency_0_1_1_1>();

	private void Setup_19() => DI.Setup()
		.Bind<IDependency_0_1_1_2>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_0_1_1_2>(out var val22);
			return val22;
		}).Root<IDependency_0_1_1_2>();

	private void Setup_20() => DI.Setup()
		.Bind<IDependency_0_1_1>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_0_1_1>(out var val11);
			return val11;
		}).Root<IDependency_0_1_1>();

	private void Setup_21() => DI.Setup()
		.Bind<IDependency_0_1_2_0>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_0_1_2_0>(out var val20);
			return val20;
		}).Root<IDependency_0_1_2_0>();

	private void Setup_22() => DI.Setup()
		.Bind<IDependency_0_1_2_1>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_0_1_2_1>(out var val21);
			return val21;
		}).Root<IDependency_0_1_2_1>();

	private void Setup_23() => DI.Setup()
		.Bind<IDependency_0_1_2_2>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_0_1_2_2>(out var val22);
			return val22;
		}).Root<IDependency_0_1_2_2>();

	private void Setup_24() => DI.Setup()
		.Bind<IDependency_0_1_2>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_0_1_2>(out var val12);
			return val12;
		}).Root<IDependency_0_1_2>();

	private void Setup_25() => DI.Setup()
		.Bind<IDependency_0_1>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_0_1>(out var val01);
			return val01;
		}).Root<IDependency_0_1>();

	private void Setup_26() => DI.Setup()
		.Bind<IDependency_0_2_0_0>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_0_2_0_0>(out var val20);
			return val20;
		}).Root<IDependency_0_2_0_0>();

	private void Setup_27() => DI.Setup()
		.Bind<IDependency_0_2_0_1>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_0_2_0_1>(out var val21);
			return val21;
		}).Root<IDependency_0_2_0_1>();

	private void Setup_28() => DI.Setup()
		.Bind<IDependency_0_2_0_2>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_0_2_0_2>(out var val22);
			return val22;
		}).Root<IDependency_0_2_0_2>();

	private void Setup_29() => DI.Setup()
		.Bind<IDependency_0_2_0>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_0_2_0>(out var val10);
			return val10;
		}).Root<IDependency_0_2_0>();

	private void Setup_30() => DI.Setup()
		.Bind<IDependency_0_2_1_0>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_0_2_1_0>(out var val20);
			return val20;
		}).Root<IDependency_0_2_1_0>();

	private void Setup_31() => DI.Setup()
		.Bind<IDependency_0_2_1_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_0_2_1_1>(out var val21);
			return val21;
		}).Root<IDependency_0_2_1_1>();

	private void Setup_32() => DI.Setup()
		.Bind<IDependency_0_2_1_2>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_0_2_1_2>(out var val22);
			return val22;
		}).Root<IDependency_0_2_1_2>();

	private void Setup_33() => DI.Setup()
		.Bind<IDependency_0_2_1>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_0_2_1>(out var val11);
			return val11;
		}).Root<IDependency_0_2_1>();

	private void Setup_34() => DI.Setup()
		.Bind<IDependency_0_2_2_0>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_0_2_2_0>(out var val20);
			return val20;
		}).Root<IDependency_0_2_2_0>();

	private void Setup_35() => DI.Setup()
		.Bind<IDependency_0_2_2_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_0_2_2_1>(out var val21);
			return val21;
		}).Root<IDependency_0_2_2_1>();

	private void Setup_36() => DI.Setup()
		.Bind<IDependency_0_2_2_2>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_0_2_2_2>(out var val22);
			return val22;
		}).Root<IDependency_0_2_2_2>();

	private void Setup_37() => DI.Setup()
		.Bind<IDependency_0_2_2>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_0_2_2>(out var val12);
			return val12;
		}).Root<IDependency_0_2_2>();

	private void Setup_38() => DI.Setup()
		.Bind<IDependency_0_2>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_0_2>(out var val02);
			return val02;
		}).Root<IDependency_0_2>();

	private void Setup_39() => DI.Setup()
		.Bind<IDependency_1_0_0_0>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_1_0_0_0>(out var val20);
			return val20;
		}).Root<IDependency_1_0_0_0>();

	private void Setup_40() => DI.Setup()
		.Bind<IDependency_1_0_0_1>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_1_0_0_1>(out var val21);
			return val21;
		}).Root<IDependency_1_0_0_1>();

	private void Setup_41() => DI.Setup()
		.Bind<IDependency_1_0_0_2>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_1_0_0_2>(out var val22);
			return val22;
		}).Root<IDependency_1_0_0_2>();

	private void Setup_42() => DI.Setup()
		.Bind<IDependency_1_0_0>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_1_0_0>(out var val10);
			return val10;
		}).Root<IDependency_1_0_0>();

	private void Setup_43() => DI.Setup()
		.Bind<IDependency_1_0_1_0>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_1_0_1_0>(out var val20);
			return val20;
		}).Root<IDependency_1_0_1_0>();

	private void Setup_44() => DI.Setup()
		.Bind<IDependency_1_0_1_1>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_1_0_1_1>(out var val21);
			return val21;
		}).Root<IDependency_1_0_1_1>();

	private void Setup_45() => DI.Setup()
		.Bind<IDependency_1_0_1_2>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_1_0_1_2>(out var val22);
			return val22;
		}).Root<IDependency_1_0_1_2>();

	private void Setup_46() => DI.Setup()
		.Bind<IDependency_1_0_1>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_1_0_1>(out var val11);
			return val11;
		}).Root<IDependency_1_0_1>();

	private void Setup_47() => DI.Setup()
		.Bind<IDependency_1_0_2_0>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_1_0_2_0>(out var val20);
			return val20;
		}).Root<IDependency_1_0_2_0>();

	private void Setup_48() => DI.Setup()
		.Bind<IDependency_1_0_2_1>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_1_0_2_1>(out var val21);
			return val21;
		}).Root<IDependency_1_0_2_1>();

	private void Setup_49() => DI.Setup()
		.Bind<IDependency_1_0_2_2>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_1_0_2_2>(out var val22);
			return val22;
		}).Root<IDependency_1_0_2_2>();

	private void Setup_50() => DI.Setup()
		.Bind<IDependency_1_0_2>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_1_0_2>(out var val12);
			return val12;
		}).Root<IDependency_1_0_2>();

	private void Setup_51() => DI.Setup()
		.Bind<IDependency_1_0>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_1_0>(out var val00);
			return val00;
		}).Root<IDependency_1_0>();

	private void Setup_52() => DI.Setup()
		.Bind<IDependency_1_1_0_0>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_1_1_0_0>(out var val20);
			return val20;
		}).Root<IDependency_1_1_0_0>();

	private void Setup_53() => DI.Setup()
		.Bind<IDependency_1_1_0_1>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_1_1_0_1>(out var val21);
			return val21;
		}).Root<IDependency_1_1_0_1>();

	private void Setup_54() => DI.Setup()
		.Bind<IDependency_1_1_0_2>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_1_1_0_2>(out var val22);
			return val22;
		}).Root<IDependency_1_1_0_2>();

	private void Setup_55() => DI.Setup()
		.Bind<IDependency_1_1_0>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_1_1_0>(out var val10);
			return val10;
		}).Root<IDependency_1_1_0>();

	private void Setup_56() => DI.Setup()
		.Bind<IDependency_1_1_1_0>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_1_1_1_0>(out var val20);
			return val20;
		}).Root<IDependency_1_1_1_0>();

	private void Setup_57() => DI.Setup()
		.Bind<IDependency_1_1_1_1>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_1_1_1_1>(out var val21);
			return val21;
		}).Root<IDependency_1_1_1_1>();

	private void Setup_58() => DI.Setup()
		.Bind<IDependency_1_1_1_2>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_1_1_1_2>(out var val22);
			return val22;
		}).Root<IDependency_1_1_1_2>();

	private void Setup_59() => DI.Setup()
		.Bind<IDependency_1_1_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_1_1_1>(out var val11);
			return val11;
		}).Root<IDependency_1_1_1>();

	private void Setup_60() => DI.Setup()
		.Bind<IDependency_1_1_2_0>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_1_1_2_0>(out var val20);
			return val20;
		}).Root<IDependency_1_1_2_0>();

	private void Setup_61() => DI.Setup()
		.Bind<IDependency_1_1_2_1>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_1_1_2_1>(out var val21);
			return val21;
		}).Root<IDependency_1_1_2_1>();

	private void Setup_62() => DI.Setup()
		.Bind<IDependency_1_1_2_2>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_1_1_2_2>(out var val22);
			return val22;
		}).Root<IDependency_1_1_2_2>();

	private void Setup_63() => DI.Setup()
		.Bind<IDependency_1_1_2>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_1_1_2>(out var val12);
			return val12;
		}).Root<IDependency_1_1_2>();

	private void Setup_64() => DI.Setup()
		.Bind<IDependency_1_1>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_1_1>(out var val01);
			return val01;
		}).Root<IDependency_1_1>();

	private void Setup_65() => DI.Setup()
		.Bind<IDependency_1_2_0_0>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_1_2_0_0>(out var val20);
			return val20;
		}).Root<IDependency_1_2_0_0>();

	private void Setup_66() => DI.Setup()
		.Bind<IDependency_1_2_0_1>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_1_2_0_1>(out var val21);
			return val21;
		}).Root<IDependency_1_2_0_1>();

	private void Setup_67() => DI.Setup()
		.Bind<IDependency_1_2_0_2>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_1_2_0_2>(out var val22);
			return val22;
		}).Root<IDependency_1_2_0_2>();

	private void Setup_68() => DI.Setup()
		.Bind<IDependency_1_2_0>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_1_2_0>(out var val10);
			return val10;
		}).Root<IDependency_1_2_0>();

	private void Setup_69() => DI.Setup()
		.Bind<IDependency_1_2_1_0>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_1_2_1_0>(out var val20);
			return val20;
		}).Root<IDependency_1_2_1_0>();

	private void Setup_70() => DI.Setup()
		.Bind<IDependency_1_2_1_1>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_1_2_1_1>(out var val21);
			return val21;
		}).Root<IDependency_1_2_1_1>();

	private void Setup_71() => DI.Setup()
		.Bind<IDependency_1_2_1_2>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_1_2_1_2>(out var val22);
			return val22;
		}).Root<IDependency_1_2_1_2>();

	private void Setup_72() => DI.Setup()
		.Bind<IDependency_1_2_1>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_1_2_1>(out var val11);
			return val11;
		}).Root<IDependency_1_2_1>();

	private void Setup_73() => DI.Setup()
		.Bind<IDependency_1_2_2_0>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_1_2_2_0>(out var val20);
			return val20;
		}).Root<IDependency_1_2_2_0>();

	private void Setup_74() => DI.Setup()
		.Bind<IDependency_1_2_2_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_1_2_2_1>(out var val21);
			return val21;
		}).Root<IDependency_1_2_2_1>();

	private void Setup_75() => DI.Setup()
		.Bind<IDependency_1_2_2_2>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_1_2_2_2>(out var val22);
			return val22;
		}).Root<IDependency_1_2_2_2>();

	private void Setup_76() => DI.Setup()
		.Bind<IDependency_1_2_2>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_1_2_2>(out var val12);
			return val12;
		}).Root<IDependency_1_2_2>();

	private void Setup_77() => DI.Setup()
		.Bind<IDependency_1_2>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_1_2>(out var val02);
			return val02;
		}).Root<IDependency_1_2>();

	private void Setup_78() => DI.Setup()
		.Bind<IDependency_2_0_0_0>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_2_0_0_0>(out var val20);
			return val20;
		}).Root<IDependency_2_0_0_0>();

	private void Setup_79() => DI.Setup()
		.Bind<IDependency_2_0_0_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_2_0_0_1>(out var val21);
			return val21;
		}).Root<IDependency_2_0_0_1>();

	private void Setup_80() => DI.Setup()
		.Bind<IDependency_2_0_0_2>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_2_0_0_2>(out var val22);
			return val22;
		}).Root<IDependency_2_0_0_2>();

	private void Setup_81() => DI.Setup()
		.Bind<IDependency_2_0_0>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_2_0_0>(out var val10);
			return val10;
		}).Root<IDependency_2_0_0>();

	private void Setup_82() => DI.Setup()
		.Bind<IDependency_2_0_1_0>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_2_0_1_0>(out var val20);
			return val20;
		}).Root<IDependency_2_0_1_0>();

	private void Setup_83() => DI.Setup()
		.Bind<IDependency_2_0_1_1>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_2_0_1_1>(out var val21);
			return val21;
		}).Root<IDependency_2_0_1_1>();

	private void Setup_84() => DI.Setup()
		.Bind<IDependency_2_0_1_2>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_2_0_1_2>(out var val22);
			return val22;
		}).Root<IDependency_2_0_1_2>();

	private void Setup_85() => DI.Setup()
		.Bind<IDependency_2_0_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_2_0_1>(out var val11);
			return val11;
		}).Root<IDependency_2_0_1>();

	private void Setup_86() => DI.Setup()
		.Bind<IDependency_2_0_2_0>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_2_0_2_0>(out var val20);
			return val20;
		}).Root<IDependency_2_0_2_0>();

	private void Setup_87() => DI.Setup()
		.Bind<IDependency_2_0_2_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_2_0_2_1>(out var val21);
			return val21;
		}).Root<IDependency_2_0_2_1>();

	private void Setup_88() => DI.Setup()
		.Bind<IDependency_2_0_2_2>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_2_0_2_2>(out var val22);
			return val22;
		}).Root<IDependency_2_0_2_2>();

	private void Setup_89() => DI.Setup()
		.Bind<IDependency_2_0_2>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_2_0_2>(out var val12);
			return val12;
		}).Root<IDependency_2_0_2>();

	private void Setup_90() => DI.Setup()
		.Bind<IDependency_2_0>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_2_0>(out var val00);
			return val00;
		}).Root<IDependency_2_0>();

	private void Setup_91() => DI.Setup()
		.Bind<IDependency_2_1_0_0>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_2_1_0_0>(out var val20);
			return val20;
		}).Root<IDependency_2_1_0_0>();

	private void Setup_92() => DI.Setup()
		.Bind<IDependency_2_1_0_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_2_1_0_1>(out var val21);
			return val21;
		}).Root<IDependency_2_1_0_1>();

	private void Setup_93() => DI.Setup()
		.Bind<IDependency_2_1_0_2>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_2_1_0_2>(out var val22);
			return val22;
		}).Root<IDependency_2_1_0_2>();

	private void Setup_94() => DI.Setup()
		.Bind<IDependency_2_1_0>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_2_1_0>(out var val10);
			return val10;
		}).Root<IDependency_2_1_0>();

	private void Setup_95() => DI.Setup()
		.Bind<IDependency_2_1_1_0>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_2_1_1_0>(out var val20);
			return val20;
		}).Root<IDependency_2_1_1_0>();

	private void Setup_96() => DI.Setup()
		.Bind<IDependency_2_1_1_1>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_2_1_1_1>(out var val21);
			return val21;
		}).Root<IDependency_2_1_1_1>();

	private void Setup_97() => DI.Setup()
		.Bind<IDependency_2_1_1_2>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_2_1_1_2>(out var val22);
			return val22;
		}).Root<IDependency_2_1_1_2>();

	private void Setup_98() => DI.Setup()
		.Bind<IDependency_2_1_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_2_1_1>(out var val11);
			return val11;
		}).Root<IDependency_2_1_1>();

	private void Setup_99() => DI.Setup()
		.Bind<IDependency_2_1_2_0>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_2_1_2_0>(out var val20);
			return val20;
		}).Root<IDependency_2_1_2_0>();

	private void Setup_100() => DI.Setup()
		.Bind<IDependency_2_1_2_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_2_1_2_1>(out var val21);
			return val21;
		}).Root<IDependency_2_1_2_1>();

	private void Setup_101() => DI.Setup()
		.Bind<IDependency_2_1_2_2>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_2_1_2_2>(out var val22);
			return val22;
		}).Root<IDependency_2_1_2_2>();

	private void Setup_102() => DI.Setup()
		.Bind<IDependency_2_1_2>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_2_1_2>(out var val12);
			return val12;
		}).Root<IDependency_2_1_2>();

	private void Setup_103() => DI.Setup()
		.Bind<IDependency_2_1>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_2_1>(out var val01);
			return val01;
		}).Root<IDependency_2_1>();

	private void Setup_104() => DI.Setup()
		.Bind<IDependency_2_2_0_0>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_2_2_0_0>(out var val20);
			return val20;
		}).Root<IDependency_2_2_0_0>();

	private void Setup_105() => DI.Setup()
		.Bind<IDependency_2_2_0_1>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_2_2_0_1>(out var val21);
			return val21;
		}).Root<IDependency_2_2_0_1>();

	private void Setup_106() => DI.Setup()
		.Bind<IDependency_2_2_0_2>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_2_2_0_2>(out var val22);
			return val22;
		}).Root<IDependency_2_2_0_2>();

	private void Setup_107() => DI.Setup()
		.Bind<IDependency_2_2_0>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_2_2_0>(out var val10);
			return val10;
		}).Root<IDependency_2_2_0>();

	private void Setup_108() => DI.Setup()
		.Bind<IDependency_2_2_1_0>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_2_2_1_0>(out var val20);
			return val20;
		}).Root<IDependency_2_2_1_0>();

	private void Setup_109() => DI.Setup()
		.Bind<IDependency_2_2_1_1>().As(Lifetime.Transient).To(ctx => {
			ctx.Inject<Dependency_2_2_1_1>(out var val21);
			return val21;
		}).Root<IDependency_2_2_1_1>();

	private void Setup_110() => DI.Setup()
		.Bind<IDependency_2_2_1_2>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_2_2_1_2>(out var val22);
			return val22;
		}).Root<IDependency_2_2_1_2>();

	private void Setup_111() => DI.Setup()
		.Bind<IDependency_2_2_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_2_2_1>(out var val11);
			return val11;
		}).Root<IDependency_2_2_1>();

	private void Setup_112() => DI.Setup()
		.Bind<IDependency_2_2_2_0>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_2_2_2_0>(out var val20);
			return val20;
		}).Root<IDependency_2_2_2_0>();

	private void Setup_113() => DI.Setup()
		.Bind<IDependency_2_2_2_1>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_2_2_2_1>(out var val21);
			return val21;
		}).Root<IDependency_2_2_2_1>();

	private void Setup_114() => DI.Setup()
		.Bind<IDependency_2_2_2_2>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_2_2_2_2>(out var val22);
			return val22;
		}).Root<IDependency_2_2_2_2>();

	private void Setup_115() => DI.Setup()
		.Bind<IDependency_2_2_2>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_2_2_2>(out var val12);
			return val12;
		}).Root<IDependency_2_2_2>();

	private void Setup_116() => DI.Setup()
		.Bind<IDependency_2_2>().As(Lifetime.Singleton).To(ctx => {
			ctx.Inject<Dependency_2_2>(out var val02);
			return val02;
		}).Root<IDependency_2_2>();

	private void Setup_117() => DI.Setup()
		.Bind<IDependency_0>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_0>(out var val0);
			return val0;
		}).Root<IDependency_0>();

	private void Setup_118() => DI.Setup()
		.Bind<IDependency_1>().As(Lifetime.PerBlock).To(ctx => {
			ctx.Inject<Dependency_1>(out var val1);
			return val1;
		}).Root<IDependency_1>();

	private void Setup_119() => DI.Setup()
		.Bind<IDependency_2>().As(Lifetime.PerResolve).To(ctx => {
			ctx.Inject<Dependency_2>(out var val2);
			return val2;
		}).Root<IDependency_2>();

}

public interface IRoot;

public class Root(IDependency_0 dep0, IDependency_1 dep1, IDependency_2 dep2): IRoot;

public interface IDependency_0;

public class Dependency_0(IDependency_0_0 dep00, IDependency_0_1 dep01, IDependency_0_2 dep02): IDependency_0;

public interface IDependency_0_0;

public class Dependency_0_0(IDependency_0_0_0 dep10, IDependency_0_0_1 dep11, IDependency_0_0_2 dep12): IDependency_0_0;

public interface IDependency_0_0_0;

public class Dependency_0_0_0(IDependency_0_0_0_0 dep20, IDependency_0_0_0_1 dep21, IDependency_0_0_0_2 dep22): IDependency_0_0_0;

public interface IDependency_0_0_0_0;

public class Dependency_0_0_0_0(): IDependency_0_0_0_0;

public interface IDependency_0_0_0_1;

public class Dependency_0_0_0_1(): IDependency_0_0_0_1;

public interface IDependency_0_0_0_2;

public class Dependency_0_0_0_2(): IDependency_0_0_0_2;

public interface IDependency_0_0_1;

public class Dependency_0_0_1(IDependency_0_0_1_0 dep20, IDependency_0_0_1_1 dep21, IDependency_0_0_1_2 dep22): IDependency_0_0_1;

public interface IDependency_0_0_1_0;

public class Dependency_0_0_1_0(): IDependency_0_0_1_0;

public interface IDependency_0_0_1_1;

public class Dependency_0_0_1_1(): IDependency_0_0_1_1;

public interface IDependency_0_0_1_2;

public class Dependency_0_0_1_2(): IDependency_0_0_1_2;

public interface IDependency_0_0_2;

public class Dependency_0_0_2(IDependency_0_0_2_0 dep20, IDependency_0_0_2_1 dep21, IDependency_0_0_2_2 dep22): IDependency_0_0_2;

public interface IDependency_0_0_2_0;

public class Dependency_0_0_2_0(): IDependency_0_0_2_0;

public interface IDependency_0_0_2_1;

public class Dependency_0_0_2_1(): IDependency_0_0_2_1;

public interface IDependency_0_0_2_2;

public class Dependency_0_0_2_2(): IDependency_0_0_2_2;

public interface IDependency_0_1;

public class Dependency_0_1(IDependency_0_1_0 dep10, IDependency_0_1_1 dep11, IDependency_0_1_2 dep12): IDependency_0_1;

public interface IDependency_0_1_0;

public class Dependency_0_1_0(IDependency_0_1_0_0 dep20, IDependency_0_1_0_1 dep21, IDependency_0_1_0_2 dep22): IDependency_0_1_0;

public interface IDependency_0_1_0_0;

public class Dependency_0_1_0_0(): IDependency_0_1_0_0;

public interface IDependency_0_1_0_1;

public class Dependency_0_1_0_1(): IDependency_0_1_0_1;

public interface IDependency_0_1_0_2;

public class Dependency_0_1_0_2(): IDependency_0_1_0_2;

public interface IDependency_0_1_1;

public class Dependency_0_1_1(IDependency_0_1_1_0 dep20, IDependency_0_1_1_1 dep21, IDependency_0_1_1_2 dep22): IDependency_0_1_1;

public interface IDependency_0_1_1_0;

public class Dependency_0_1_1_0(): IDependency_0_1_1_0;

public interface IDependency_0_1_1_1;

public class Dependency_0_1_1_1(): IDependency_0_1_1_1;

public interface IDependency_0_1_1_2;

public class Dependency_0_1_1_2(): IDependency_0_1_1_2;

public interface IDependency_0_1_2;

public class Dependency_0_1_2(IDependency_0_1_2_0 dep20, IDependency_0_1_2_1 dep21, IDependency_0_1_2_2 dep22): IDependency_0_1_2;

public interface IDependency_0_1_2_0;

public class Dependency_0_1_2_0(): IDependency_0_1_2_0;

public interface IDependency_0_1_2_1;

public class Dependency_0_1_2_1(): IDependency_0_1_2_1;

public interface IDependency_0_1_2_2;

public class Dependency_0_1_2_2(): IDependency_0_1_2_2;

public interface IDependency_0_2;

public class Dependency_0_2(IDependency_0_2_0 dep10, IDependency_0_2_1 dep11, IDependency_0_2_2 dep12): IDependency_0_2;

public interface IDependency_0_2_0;

public class Dependency_0_2_0(IDependency_0_2_0_0 dep20, IDependency_0_2_0_1 dep21, IDependency_0_2_0_2 dep22): IDependency_0_2_0;

public interface IDependency_0_2_0_0;

public class Dependency_0_2_0_0(): IDependency_0_2_0_0;

public interface IDependency_0_2_0_1;

public class Dependency_0_2_0_1(): IDependency_0_2_0_1;

public interface IDependency_0_2_0_2;

public class Dependency_0_2_0_2(): IDependency_0_2_0_2;

public interface IDependency_0_2_1;

public class Dependency_0_2_1(IDependency_0_2_1_0 dep20, IDependency_0_2_1_1 dep21, IDependency_0_2_1_2 dep22): IDependency_0_2_1;

public interface IDependency_0_2_1_0;

public class Dependency_0_2_1_0(): IDependency_0_2_1_0;

public interface IDependency_0_2_1_1;

public class Dependency_0_2_1_1(): IDependency_0_2_1_1;

public interface IDependency_0_2_1_2;

public class Dependency_0_2_1_2(): IDependency_0_2_1_2;

public interface IDependency_0_2_2;

public class Dependency_0_2_2(IDependency_0_2_2_0 dep20, IDependency_0_2_2_1 dep21, IDependency_0_2_2_2 dep22): IDependency_0_2_2;

public interface IDependency_0_2_2_0;

public class Dependency_0_2_2_0(): IDependency_0_2_2_0;

public interface IDependency_0_2_2_1;

public class Dependency_0_2_2_1(): IDependency_0_2_2_1;

public interface IDependency_0_2_2_2;

public class Dependency_0_2_2_2(): IDependency_0_2_2_2;

public interface IDependency_1;

public class Dependency_1(IDependency_1_0 dep00, IDependency_1_1 dep01, IDependency_1_2 dep02): IDependency_1;

public interface IDependency_1_0;

public class Dependency_1_0(IDependency_1_0_0 dep10, IDependency_1_0_1 dep11, IDependency_1_0_2 dep12): IDependency_1_0;

public interface IDependency_1_0_0;

public class Dependency_1_0_0(IDependency_1_0_0_0 dep20, IDependency_1_0_0_1 dep21, IDependency_1_0_0_2 dep22): IDependency_1_0_0;

public interface IDependency_1_0_0_0;

public class Dependency_1_0_0_0(): IDependency_1_0_0_0;

public interface IDependency_1_0_0_1;

public class Dependency_1_0_0_1(): IDependency_1_0_0_1;

public interface IDependency_1_0_0_2;

public class Dependency_1_0_0_2(): IDependency_1_0_0_2;

public interface IDependency_1_0_1;

public class Dependency_1_0_1(IDependency_1_0_1_0 dep20, IDependency_1_0_1_1 dep21, IDependency_1_0_1_2 dep22): IDependency_1_0_1;

public interface IDependency_1_0_1_0;

public class Dependency_1_0_1_0(): IDependency_1_0_1_0;

public interface IDependency_1_0_1_1;

public class Dependency_1_0_1_1(): IDependency_1_0_1_1;

public interface IDependency_1_0_1_2;

public class Dependency_1_0_1_2(): IDependency_1_0_1_2;

public interface IDependency_1_0_2;

public class Dependency_1_0_2(IDependency_1_0_2_0 dep20, IDependency_1_0_2_1 dep21, IDependency_1_0_2_2 dep22): IDependency_1_0_2;

public interface IDependency_1_0_2_0;

public class Dependency_1_0_2_0(): IDependency_1_0_2_0;

public interface IDependency_1_0_2_1;

public class Dependency_1_0_2_1(): IDependency_1_0_2_1;

public interface IDependency_1_0_2_2;

public class Dependency_1_0_2_2(): IDependency_1_0_2_2;

public interface IDependency_1_1;

public class Dependency_1_1(IDependency_1_1_0 dep10, IDependency_1_1_1 dep11, IDependency_1_1_2 dep12): IDependency_1_1;

public interface IDependency_1_1_0;

public class Dependency_1_1_0(IDependency_1_1_0_0 dep20, IDependency_1_1_0_1 dep21, IDependency_1_1_0_2 dep22): IDependency_1_1_0;

public interface IDependency_1_1_0_0;

public class Dependency_1_1_0_0(): IDependency_1_1_0_0;

public interface IDependency_1_1_0_1;

public class Dependency_1_1_0_1(): IDependency_1_1_0_1;

public interface IDependency_1_1_0_2;

public class Dependency_1_1_0_2(): IDependency_1_1_0_2;

public interface IDependency_1_1_1;

public class Dependency_1_1_1(IDependency_1_1_1_0 dep20, IDependency_1_1_1_1 dep21, IDependency_1_1_1_2 dep22): IDependency_1_1_1;

public interface IDependency_1_1_1_0;

public class Dependency_1_1_1_0(): IDependency_1_1_1_0;

public interface IDependency_1_1_1_1;

public class Dependency_1_1_1_1(): IDependency_1_1_1_1;

public interface IDependency_1_1_1_2;

public class Dependency_1_1_1_2(): IDependency_1_1_1_2;

public interface IDependency_1_1_2;

public class Dependency_1_1_2(IDependency_1_1_2_0 dep20, IDependency_1_1_2_1 dep21, IDependency_1_1_2_2 dep22): IDependency_1_1_2;

public interface IDependency_1_1_2_0;

public class Dependency_1_1_2_0(): IDependency_1_1_2_0;

public interface IDependency_1_1_2_1;

public class Dependency_1_1_2_1(): IDependency_1_1_2_1;

public interface IDependency_1_1_2_2;

public class Dependency_1_1_2_2(): IDependency_1_1_2_2;

public interface IDependency_1_2;

public class Dependency_1_2(IDependency_1_2_0 dep10, IDependency_1_2_1 dep11, IDependency_1_2_2 dep12): IDependency_1_2;

public interface IDependency_1_2_0;

public class Dependency_1_2_0(IDependency_1_2_0_0 dep20, IDependency_1_2_0_1 dep21, IDependency_1_2_0_2 dep22): IDependency_1_2_0;

public interface IDependency_1_2_0_0;

public class Dependency_1_2_0_0(): IDependency_1_2_0_0;

public interface IDependency_1_2_0_1;

public class Dependency_1_2_0_1(): IDependency_1_2_0_1;

public interface IDependency_1_2_0_2;

public class Dependency_1_2_0_2(): IDependency_1_2_0_2;

public interface IDependency_1_2_1;

public class Dependency_1_2_1(IDependency_1_2_1_0 dep20, IDependency_1_2_1_1 dep21, IDependency_1_2_1_2 dep22): IDependency_1_2_1;

public interface IDependency_1_2_1_0;

public class Dependency_1_2_1_0(): IDependency_1_2_1_0;

public interface IDependency_1_2_1_1;

public class Dependency_1_2_1_1(): IDependency_1_2_1_1;

public interface IDependency_1_2_1_2;

public class Dependency_1_2_1_2(): IDependency_1_2_1_2;

public interface IDependency_1_2_2;

public class Dependency_1_2_2(IDependency_1_2_2_0 dep20, IDependency_1_2_2_1 dep21, IDependency_1_2_2_2 dep22): IDependency_1_2_2;

public interface IDependency_1_2_2_0;

public class Dependency_1_2_2_0(): IDependency_1_2_2_0;

public interface IDependency_1_2_2_1;

public class Dependency_1_2_2_1(): IDependency_1_2_2_1;

public interface IDependency_1_2_2_2;

public class Dependency_1_2_2_2(): IDependency_1_2_2_2;

public interface IDependency_2;

public class Dependency_2(IDependency_2_0 dep00, IDependency_2_1 dep01, IDependency_2_2 dep02): IDependency_2;

public interface IDependency_2_0;

public class Dependency_2_0(IDependency_2_0_0 dep10, IDependency_2_0_1 dep11, IDependency_2_0_2 dep12): IDependency_2_0;

public interface IDependency_2_0_0;

public class Dependency_2_0_0(IDependency_2_0_0_0 dep20, IDependency_2_0_0_1 dep21, IDependency_2_0_0_2 dep22): IDependency_2_0_0;

public interface IDependency_2_0_0_0;

public class Dependency_2_0_0_0(): IDependency_2_0_0_0;

public interface IDependency_2_0_0_1;

public class Dependency_2_0_0_1(): IDependency_2_0_0_1;

public interface IDependency_2_0_0_2;

public class Dependency_2_0_0_2(): IDependency_2_0_0_2;

public interface IDependency_2_0_1;

public class Dependency_2_0_1(IDependency_2_0_1_0 dep20, IDependency_2_0_1_1 dep21, IDependency_2_0_1_2 dep22): IDependency_2_0_1;

public interface IDependency_2_0_1_0;

public class Dependency_2_0_1_0(): IDependency_2_0_1_0;

public interface IDependency_2_0_1_1;

public class Dependency_2_0_1_1(): IDependency_2_0_1_1;

public interface IDependency_2_0_1_2;

public class Dependency_2_0_1_2(): IDependency_2_0_1_2;

public interface IDependency_2_0_2;

public class Dependency_2_0_2(IDependency_2_0_2_0 dep20, IDependency_2_0_2_1 dep21, IDependency_2_0_2_2 dep22): IDependency_2_0_2;

public interface IDependency_2_0_2_0;

public class Dependency_2_0_2_0(): IDependency_2_0_2_0;

public interface IDependency_2_0_2_1;

public class Dependency_2_0_2_1(): IDependency_2_0_2_1;

public interface IDependency_2_0_2_2;

public class Dependency_2_0_2_2(): IDependency_2_0_2_2;

public interface IDependency_2_1;

public class Dependency_2_1(IDependency_2_1_0 dep10, IDependency_2_1_1 dep11, IDependency_2_1_2 dep12): IDependency_2_1;

public interface IDependency_2_1_0;

public class Dependency_2_1_0(IDependency_2_1_0_0 dep20, IDependency_2_1_0_1 dep21, IDependency_2_1_0_2 dep22): IDependency_2_1_0;

public interface IDependency_2_1_0_0;

public class Dependency_2_1_0_0(): IDependency_2_1_0_0;

public interface IDependency_2_1_0_1;

public class Dependency_2_1_0_1(): IDependency_2_1_0_1;

public interface IDependency_2_1_0_2;

public class Dependency_2_1_0_2(): IDependency_2_1_0_2;

public interface IDependency_2_1_1;

public class Dependency_2_1_1(IDependency_2_1_1_0 dep20, IDependency_2_1_1_1 dep21, IDependency_2_1_1_2 dep22): IDependency_2_1_1;

public interface IDependency_2_1_1_0;

public class Dependency_2_1_1_0(): IDependency_2_1_1_0;

public interface IDependency_2_1_1_1;

public class Dependency_2_1_1_1(): IDependency_2_1_1_1;

public interface IDependency_2_1_1_2;

public class Dependency_2_1_1_2(): IDependency_2_1_1_2;

public interface IDependency_2_1_2;

public class Dependency_2_1_2(IDependency_2_1_2_0 dep20, IDependency_2_1_2_1 dep21, IDependency_2_1_2_2 dep22): IDependency_2_1_2;

public interface IDependency_2_1_2_0;

public class Dependency_2_1_2_0(): IDependency_2_1_2_0;

public interface IDependency_2_1_2_1;

public class Dependency_2_1_2_1(): IDependency_2_1_2_1;

public interface IDependency_2_1_2_2;

public class Dependency_2_1_2_2(): IDependency_2_1_2_2;

public interface IDependency_2_2;

public class Dependency_2_2(IDependency_2_2_0 dep10, IDependency_2_2_1 dep11, IDependency_2_2_2 dep12): IDependency_2_2;

public interface IDependency_2_2_0;

public class Dependency_2_2_0(IDependency_2_2_0_0 dep20, IDependency_2_2_0_1 dep21, IDependency_2_2_0_2 dep22): IDependency_2_2_0;

public interface IDependency_2_2_0_0;

public class Dependency_2_2_0_0(): IDependency_2_2_0_0;

public interface IDependency_2_2_0_1;

public class Dependency_2_2_0_1(): IDependency_2_2_0_1;

public interface IDependency_2_2_0_2;

public class Dependency_2_2_0_2(): IDependency_2_2_0_2;

public interface IDependency_2_2_1;

public class Dependency_2_2_1(IDependency_2_2_1_0 dep20, IDependency_2_2_1_1 dep21, IDependency_2_2_1_2 dep22): IDependency_2_2_1;

public interface IDependency_2_2_1_0;

public class Dependency_2_2_1_0(): IDependency_2_2_1_0;

public interface IDependency_2_2_1_1;

public class Dependency_2_2_1_1(): IDependency_2_2_1_1;

public interface IDependency_2_2_1_2;

public class Dependency_2_2_1_2(): IDependency_2_2_1_2;

public interface IDependency_2_2_2;

public class Dependency_2_2_2(IDependency_2_2_2_0 dep20, IDependency_2_2_2_1 dep21, IDependency_2_2_2_2 dep22): IDependency_2_2_2;

public interface IDependency_2_2_2_0;

public class Dependency_2_2_2_0(): IDependency_2_2_2_0;

public interface IDependency_2_2_2_1;

public class Dependency_2_2_2_1(): IDependency_2_2_2_1;

public interface IDependency_2_2_2_2;

public class Dependency_2_2_2_2(): IDependency_2_2_2_2;


