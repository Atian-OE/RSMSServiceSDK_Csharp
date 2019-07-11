using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSMSServiceSDK
{
    public class Codec
    {
        public static byte[] Encode(object obj) {
            Type type = obj.GetType();
            byte[] data= Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj));
            byte[] cache = new byte[data.Length + 5];
            byte[] leng= BitConverter.GetBytes(data.Length);
            if (BitConverter.IsLittleEndian) {
                Array.Reverse(leng);
            }
            Array.Copy(leng, cache, leng.Length);

            if (type == typeof(GetDefenceZoneRequest))
            {
                cache[4] = (byte)MsgID.GetDefenceZoneRequestID;
            }
            else if (type == typeof(GetHistoryRequest))
            {
                cache[4] = (byte)MsgID.GetHistoryRequestID;
            }
            else if (type == typeof(DeploymentRequest))
            {
                cache[4] = (byte)MsgID.DeploymentRequestID;
            }
            else if (type == typeof(WithdrawalRequest))
            {
                cache[4] = (byte)MsgID.WithdrawalRequestID;
            }
            else if (type == typeof(ResetAlarmRequest))
            {
                cache[4] = (byte)MsgID.ResetAlarmRequestID;
            }
            else if (type == typeof(CloseAlarmSoundRequest))
            {
                cache[4] = (byte)MsgID.CloseAlarmSoundRequestID;
            }
            else if (type == typeof(HeartBeat)) {
                cache[4] = (byte)MsgID.HeartBeatID;
            }
            Array.Copy(data,0, cache,5, data.Length);
            return cache;
        }
    }
}
