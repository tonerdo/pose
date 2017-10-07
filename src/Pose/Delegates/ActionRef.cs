using System;

namespace Pose.Delegates
{
    public delegate void ActionRef<T>(ref T arg);
    public delegate void ActionRef<T1, T2>(ref T1 arg1, T2 arg2);
    public delegate void ActionRef<T1, T2, T3>(ref T1 arg1, T2 arg2, T3 arg3);
    public delegate void ActionRef<T1, T2, T3, T4>(ref T1 arg1, T2 arg2, T3 arg3, T4 arg4);
    public delegate void ActionRef<T1, T2, T3, T4, T5>(ref T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
    public delegate void ActionRef<T1, T2, T3, T4, T5, T6>(ref T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
    public delegate void ActionRef<T1, T2, T3, T4, T5, T6, T7>(ref T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);
    public delegate void ActionRef<T1, T2, T3, T4, T5, T6, T7, T8>(ref T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8);
    public delegate void ActionRef<T1, T2, T3, T4, T5, T6, T7, T8, T9>(ref T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9);
    public delegate void ActionRef<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(ref T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10);
}