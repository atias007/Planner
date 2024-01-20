﻿using System;

namespace Planar.Client.Exceptions
{
    public sealed class PlanarConflictException : Exception
    {
        internal PlanarConflictException()
        {
        }

        internal PlanarConflictException(string message) : base(message)
        {
        }
    }
}