﻿using System;
using System.Runtime.Serialization;

namespace Planar.Service.Exceptions
{
    [Serializable]
    public class RestServiceUnavailableException : Exception
    {
        public RestServiceUnavailableException(string message) : base(message)
        {
        }

        protected RestServiceUnavailableException(SerializationInfo info, StreamingContext context)
        {
            // ...
        }

        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}