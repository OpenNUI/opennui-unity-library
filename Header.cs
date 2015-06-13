using System;
using System.Collections.Generic;
using System.Text;

namespace OpenNUI.Unity.Library
{
    // Server To Client Header
    enum STCHeader : short
    {
        REQUEST_DEVICE_KIND_TCP = 0x01,
        SEND_PIPE_CONNECT_TCP = 0x02,
        TEST_TEXTMESSAGE = 0x03,
        SEND_COLORFRAME_SM_NAME = 0x04,
        SEND_DEPTHFRAME_SM_NAME = 0x05,
        SEND_BODY_SM_NAME = 0x06,
        SEND_ALL_SENSOR_INFO = 0x07,
        SEND_NEW_SENSOR_INFO = 0x08,
        SEND_SENSOR_STATE = 0x09,
        SEND_TRIGGER_EVENT_DATA     = 0x0010,
    }

    //  Client To Server Header
    enum CTSHeader : short
    {
        SEND_DEVICE_KIND_TCP = 0x01,
        SEND_PIPE_CONNECT_COMPLETE = 0x02,
        TEST_TEXTMESSAGE = 0x03,
        REQUEST_COLORFRAME_SM_NAME = 0x04,
        REQUEST_DEPTHFRAME_SM_NAME = 0x05,
        REQUEST_BODY_SM_NAME = 0x06,

    }
}
