﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Reloaded.Assembler;

/// <summary/>
[ExcludeFromCodeCoverage]
public class FasmWrapperException : Exception
{
    /// <summary/>
    public FasmWrapperException() { }

    /// <summary/>
    public FasmWrapperException(string message) : base(message) { }

    /// <summary/>
    public FasmWrapperException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary/>
    protected FasmWrapperException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
