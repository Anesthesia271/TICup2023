using System;
using System.Collections.Generic;

namespace TICup2023.Tool.Helper;

public static class EnumHelper<T>
{
    public static List<T> ToList() => new(Enum.GetValues(typeof(T)) as T[] ?? Array.Empty<T>());
}