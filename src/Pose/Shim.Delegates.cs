using System;

using Pose.Delegates;

namespace Pose
{
    public partial class Shim
    {
        public Shim With(Delegate replacement) => WithImpl(replacement);

        public Shim With(Action replacement) => WithImpl(replacement);

        public Shim With<T>(Action<T> replacement) => WithImpl(replacement);

        public Shim With<T>(ActionRef<T> replacement) => WithImpl(replacement);

        public Shim With<T1, T2>(Action<T1, T2> replacement) => WithImpl(replacement);

        public Shim With<T1, T2>(ActionRef<T1, T2> replacement) => WithImpl(replacement);

        public Shim With<T1, T2, T3>(Action<T1, T2, T3> replacement) => WithImpl(replacement);

        public Shim With<T1, T2, T3>(ActionRef<T1, T2, T3> replacement) => WithImpl(replacement);

        public Shim With<T1, T2, T3, T4>(Action<T1, T2, T3, T4> replacement)
            => WithImpl(replacement);

        public Shim With<T1, T2, T3, T4>(ActionRef<T1, T2, T3, T4> replacement)
            => WithImpl(replacement);

        public Shim With<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> replacement)
            => WithImpl(replacement);

        public Shim With<T1, T2, T3, T4, T5>(ActionRef<T1, T2, T3, T4, T5> replacement)
            => WithImpl(replacement);

        public Shim With<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> replacement)
            => WithImpl(replacement);

        public Shim With<T1, T2, T3, T4, T5, T6>(ActionRef<T1, T2, T3, T4, T5, T6> replacement)
            => WithImpl(replacement);

        public Shim With<T1, T2, T3, T4, T5, T6, T7>(Action<T1, T2, T3, T4, T5, T6, T7> replacement)
            => WithImpl(replacement);

        public Shim With<T1, T2, T3, T4, T5, T6, T7>(ActionRef<T1, T2, T3, T4, T5, T6, T7> replacement)
            => WithImpl(replacement);

        public Shim With<T1, T2, T3, T4, T5, T6, T7, T8>(Action<T1, T2, T3, T4, T5, T6, T7, T8> replacement)
            => WithImpl(replacement);

        public Shim With<T1, T2, T3, T4, T5, T6, T7, T8>(ActionRef<T1, T2, T3, T4, T5, T6, T7, T8> replacement)
            => WithImpl(replacement);

        public Shim With<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> replacement)
            => WithImpl(replacement);

        public Shim With<T1, T2, T3, T4, T5, T6, T7, T8, T9>(ActionRef<T1, T2, T3, T4, T5, T6, T7, T8, T9> replacement)
            => WithImpl(replacement);

        public Shim With<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> replacement)
            => WithImpl(replacement);

        public Shim With<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(ActionRef<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> replacement)
            => WithImpl(replacement);

        public Shim With<TResult>(Func<TResult> replacement) => WithImpl(replacement);

        public Shim With<T1, TResult>(Func<T1, TResult> replacement) => WithImpl(replacement);

        public Shim With<T1, TResult>(FuncRef<T1, TResult> replacement) => WithImpl(replacement);

        public Shim With<T1, T2, TResult>(Func<T1, T2, TResult> replacement) => WithImpl(replacement);

        public Shim With<T1, T2, TResult>(FuncRef<T1, T2, TResult> replacement) => WithImpl(replacement);

        public Shim With<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> replacement) => WithImpl(replacement);

        public Shim With<T1, T2, T3, TResult>(FuncRef<T1, T2, T3, TResult> replacement) => WithImpl(replacement);

        public Shim With<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> replacement)
            => WithImpl(replacement);

        public Shim With<T1, T2, T3, T4, TResult>(FuncRef<T1, T2, T3, T4, TResult> replacement)
            => WithImpl(replacement);

        public Shim With<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, TResult> replacement)
            => WithImpl(replacement);

        public Shim With<T1, T2, T3, T4, T5, TResult>(FuncRef<T1, T2, T3, T4, T5, TResult> replacement)
            => WithImpl(replacement);

        public Shim With<T1, T2, T3, T4, T5, T6, TResult>(Func<T1, T2, T3, T4, T5, T6, TResult> replacement)
            => WithImpl(replacement);

        public Shim With<T1, T2, T3, T4, T5, T6, TResult>(FuncRef<T1, T2, T3, T4, T5, T6, TResult> replacement)
            => WithImpl(replacement);

        public Shim With<T1, T2, T3, T4, T5, T6, T7, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, TResult> replacement)
            => WithImpl(replacement);

        public Shim With<T1, T2, T3, T4, T5, T6, T7, TResult>(FuncRef<T1, T2, T3, T4, T5, T6, T7, TResult> replacement)
            => WithImpl(replacement);

        public Shim With<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> replacement)
            => WithImpl(replacement);

        public Shim With<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(FuncRef<T1, T2, T3, T4, T5, T6, T7, T8, TResult> replacement)
            => WithImpl(replacement);

        public Shim With<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> replacement)
            => WithImpl(replacement);

        public Shim With<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(FuncRef<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> replacement)
            => WithImpl(replacement);

        public Shim With<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> replacement)
            => WithImpl(replacement);

        public Shim With<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(FuncRef<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> replacement)
            => WithImpl(replacement);
    }
}