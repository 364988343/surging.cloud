﻿namespace Surging.Cloud.CPlatform.Exceptions
{
    public class LockerTimeoutException : CPlatformException
    {
        public LockerTimeoutException(string message, StatusCode status = StatusCode.LockerTimeout) : base(message, status)
        {
        }
    }
}
