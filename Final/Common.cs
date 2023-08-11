using System;
using System.Net;



class Common
{
    public static string IpAddrToString(UInt32 ipAddress)
    {
        UInt32 hostOrderAddress = (UInt32)IPAddress.NetworkToHostOrder((Int32)ipAddress);
        IPAddress ip = new IPAddress(hostOrderAddress);

        return ip.ToString();
    }



    public static string MacAddrToString(UInt64 macAddress)
    {
        string strMac;
        UInt32 temp1, temp2, temp3, temp4, temp5, temp6;

        // MAC Address has 8 Bytes
        temp1 = (UInt32)(macAddress >> 40) & 0xFF;
        temp2 = (UInt32)(macAddress >> 32) & 0xFF;
        temp3 = (UInt32)(macAddress >> 24) & 0xFF;
        temp4 = (UInt32)(macAddress >> 16) & 0xFF;
        temp5 = (UInt32)(macAddress >> 8) & 0xFF;
        temp6 = (UInt32)(macAddress) & 0xFF;

        strMac = temp1.ToString("X2") + ":" + temp2.ToString("X2") + ":" + temp3.ToString("X2") + ":" + temp4.ToString("X2") + ":" + temp5.ToString("X2") + ":" + temp6.ToString("X2");

        return strMac;
    }
}