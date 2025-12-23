using System;
using System.Collections.Generic;

namespace SystemScrap.ServiceLocator.Framework
{
    internal delegate Dictionary<Type, object> NewScope<in T>(T obj);
}