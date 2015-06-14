using System;
using System.Collections.Generic;
using System.Text;

namespace OpenNUI.Unity.Library
{
    public class AuthorityException : Exception
    {
        public enum AuthorityTypes
        {
            Color, Depth, Body
        }
        private AuthorityTypes noAuthority;
        public AuthorityTypes NoAuthority
        {
            get { return noAuthority; }
        }
        public NuiSensor Sensor { get; private set; }

        internal AuthorityException(AuthorityTypes noAuthority, NuiSensor sensor)
        {
            this.noAuthority = noAuthority;
            this.Sensor = sensor;
        }

        public override string Message
        {
            get
            {
                return "O-NUI : " + Sensor.name + "(" + Sensor.id + ") is dosen't have " + noAuthority.ToString() + " frame authority";
            }
        }
    }
    public class NotConnectedExeption : Exception
    {
        public override string Message
        {
            get
            {
                return "O-NUI : " + "NUI Gateway Library is not connected.";
            }
        }
    }

    public class MessageExeption : Exception
    {
        private string message;
        private MessageExeption(string message)
        {
            this.message = message;
        }

        public override string Message
        {
            get
            {
                return "O-NUI : " + message;
            }
        }
    }
}
