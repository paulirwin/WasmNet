namespace WasmNet.Core;

public static class SelectFunctions
{
    public static T Select<T>(T val1, T val2, int c) => c == 0 ? val2 : val1;
}