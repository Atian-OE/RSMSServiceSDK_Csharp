using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace RSMSServiceSDK
{
    public class RSMSServiceClient
    {
        private struct WaitPack
        {
            public Action<MsgID, string> action;
            public MsgID msg_id;
        }

        /// <summary>
        /// 连接的回调
        /// </summary>
        public Action<String> ConnectedAction;
        /// <summary>
        /// 断开的回调
        /// </summary>
        public Action<String> DisconnectedAction;
        /// <summary>
        /// 异常回调
        /// </summary>
        public Action<Exception> ErrorAction;
        /// <summary>
        /// 创建防区通知
        /// </summary>
        public Action<CreateDefenceZoneNotify> CreateDefenceZoneAction;
        /// <summary>
        /// 更新防区通知
        /// </summary>
        public Action<UpdateDefenceZoneNotify> UpdateDefenceZoneAction;
        /// <summary>
        /// 更新防区状态通知
        /// </summary>
        public Action<UpdateDFStateNotify> UpdateDFStateAction;
        /// <summary>
        /// 更新防区布防状态通知
        /// </summary>
        public Action<UpdateDFDeploymentStateNotify> UpdateDFDeploymentStateAction;
        /// <summary>
        /// 删除防区通知
        /// </summary>
        public Action<DeleteDFNotify> DeleteDFAction;

        private string host = "127.0.0.1";
        private bool is_connected = false;
        private TcpClient tcp_client;
        //自动连接线程
        private Thread auto_connect_th = null;
        private bool break_auto_connect_th = false;
        //心跳线程
        private Thread heart_beat_th = null;
        private bool break_heart_beat_th = false;
        //接受线程
        private Thread receive_th = null;
        private bool break_receive_th = false;

        //接受数据的缓存
        private ByteBuffer recv_buf = ByteBuffer.Allocate(1024);
        //等待这个包的回传
        private LinkedList<WaitPack> wait_pack = new LinkedList<WaitPack>();
        private object wait_pack_look = new object();

        /// <summary>
        /// 初始化连接
        /// </summary>
        /// <param name="_host">远端设备ip地址</param>
        public RSMSServiceClient(string _host)
        {
            host = _host;
        }

        /// <summary>
        /// 开始
        /// </summary>
        public void Start() {

            Close();

            auto_connect_th = new Thread(auto_connect);
            auto_connect_th.Start();

            heart_beat_th = new Thread(heart_beat);
            heart_beat_th.Start();

            receive_th = new Thread(receive);
            receive_th.Start();
        }

        /// <summary>
        /// 销毁SDK
        /// </summary>
        public void Close()
        {
            lock (wait_pack_look)
            {
                wait_pack.Clear();
            }

            if (auto_connect_th != null)
            {
                break_auto_connect_th = true;
                auto_connect_th.Join();
            }

            if (heart_beat_th != null)
            {
                break_heart_beat_th = true;
                heart_beat_th.Join();
            }

            if (is_connected)
            {
                disconnect();
            }

            if (receive_th != null)
            {
                break_receive_th = true;
                receive_th.Join();
            }
        }

        //断开连接
        private void disconnect()
        {
            if (is_connected)
            {
                tcp_client.Close();
                is_connected = false;
                receive_handle(MsgID.DisconnectID, null);
            }
        }

        //写入数据到socket
        private ErrorDef write(byte[] data)
        {
            if (is_connected)
            {
                try
                {
                    tcp_client.GetStream().Write(data, 0, data.Length);
                }
                catch (Exception ex)
                {
                    if (ErrorAction != null)
                    {
                        ErrorAction.Invoke(ex);
                    }
                    is_connected = false;
                    return ErrorDef.SendErr;
                }
                return ErrorDef.Ok;
            }
            else
            {
                return ErrorDef.UnconnectedErr;
            }
        }

        //自动连接
        private void auto_connect(object o)
        {
            break_auto_connect_th = false;
            int reconnect_time_full = 10;
            int reconnect_time = 10;
            while (!break_auto_connect_th)
            {

                if (reconnect_time == reconnect_time_full)
                {
                    if (!is_connected)
                    {
                        try
                        {
                            tcp_client = new TcpClient();
                            tcp_client.SendTimeout = 2000;
                            tcp_client.ReceiveTimeout = 0;
                            tcp_client.Connect(host, 17082);
                            is_connected = tcp_client.Connected;
                            receive_handle(MsgID.ConnectID, null);
                        }
                        catch (Exception ex)
                        {
                            if (ErrorAction != null)
                            {
                                ErrorAction.Invoke(ex);
                            }
                            is_connected = false;
                        }
                    }

                    reconnect_time = 0;
                }
                reconnect_time++;
                Thread.Sleep(1000);
            }
            auto_connect_th = null;
        }

        //心跳
        private void heart_beat(object o)
        {
            break_heart_beat_th = false;
            int heart_beat_time_full = 5;
            int heart_beat_time = 5;
            while (!break_heart_beat_th)
            {
                if (heart_beat_time == heart_beat_time_full)
                {
                    if (is_connected)
                    {
                        HeartBeat model = new HeartBeat();
                        byte[] bytes = Codec.Encode(model);
                        write(bytes);
                    }

                    heart_beat_time = 0;
                }
                heart_beat_time++;
                Thread.Sleep(1000);
            }
            heart_beat_th = null;
        }

        //接受
        private void receive(object o)
        {
            break_receive_th = false;
            recv_buf.Clear();
            byte[] buf = new byte[1024];
            while (!break_receive_th)
            {
                if (!is_connected)
                {
                    Thread.Sleep(1000);
                    continue;
                }
                try
                {
                    int n = tcp_client.GetStream().Read(buf, 0, buf.Length);
                    recv_buf.WriteBytes(buf, n);
handle_buf:
                    if (recv_buf.ReadableBytes() < 5)
                    {
                        continue;
                    }
                    recv_buf.MarkReaderIndex();
                    int pkg_size = recv_buf.ReadInt();
                    byte cmd = recv_buf.ReadByte();
                    //不够
                    if (recv_buf.ReadableBytes() < pkg_size)
                    {
                        recv_buf.ResetReaderIndex();
                        continue;
                    }
                    byte[] bytes = new byte[pkg_size];
                    recv_buf.ReadBytes(bytes, 0, pkg_size);
                    receive_handle((MsgID)cmd, bytes);

                    recv_buf.DiscardReadBytes();
                    goto handle_buf;
                }
                catch (Exception ex)
                {
                    is_connected = false;
                    if (ErrorAction != null)
                    {
                        ErrorAction.Invoke(ex);
                    }
                }
            }
            receive_th = null;
        }


        //接受处理
        private void receive_handle(MsgID cmd_id, byte[] data)
        {
            string content = "";
            if (data != null)
            {
                content = Encoding.UTF8.GetString(data);
            }
            lock (wait_pack_look)
            {
                foreach (var item in wait_pack)
                {
                    if (item.msg_id == cmd_id)
                    {
                        item.action.Invoke(cmd_id, content);
                        return;
                    }
                }
            }

            switch (cmd_id)
            {
                case MsgID.ConnectID:
                    {
                        if (ConnectedAction != null)
                        {
                            ConnectedAction.Invoke(host);
                        }
                    }
                    break;
                case MsgID.DisconnectID:
                    {
                        lock (wait_pack_look)
                        {
                            wait_pack.Clear();
                        }
                        recv_buf.Clear();
                        if (DisconnectedAction != null)
                        {
                            DisconnectedAction.Invoke(host);
                        }

                    }
                    break;
                case MsgID.CreateDefenceZoneNotifyID:
                    {
                        CreateDefenceZoneNotify notify = JsonConvert.DeserializeObject<CreateDefenceZoneNotify>(content);
                        if (notify == null)
                        {
                            return;
                        }
                        if (CreateDefenceZoneAction != null) {
                            CreateDefenceZoneAction.Invoke(notify);
                        }
                    }
                    break;
                case MsgID.UpdateDefenceZoneNotifyID:
                    {
                        UpdateDefenceZoneNotify notify = JsonConvert.DeserializeObject<UpdateDefenceZoneNotify>(content);
                        if (notify == null)
                        {
                            return;
                        }
                        if (UpdateDefenceZoneAction != null)
                        {
                            UpdateDefenceZoneAction.Invoke(notify);
                        }
                    }
                    break;
                case MsgID.UpdateDFStateNotifyID:
                    {
                        UpdateDFStateNotify notify = JsonConvert.DeserializeObject<UpdateDFStateNotify>(content);
                        if (notify == null)
                        {
                            return;
                        }
                        if (UpdateDFStateAction != null)
                        {
                            UpdateDFStateAction.Invoke(notify);
                        }
                    }
                    break;
                case MsgID.UpdateDFDeploymentStateNotifyID:
                    {
                        UpdateDFDeploymentStateNotify notify = JsonConvert.DeserializeObject<UpdateDFDeploymentStateNotify>(content);
                        if (notify == null)
                        {
                            return;
                        }
                        if (UpdateDFDeploymentStateAction != null)
                        {
                            UpdateDFDeploymentStateAction.Invoke(notify);
                        }
                    }
                    break;
                case MsgID.DeleteDFNotifyID:
                    {
                        DeleteDFNotify notify = JsonConvert.DeserializeObject<DeleteDFNotify>(content);
                        if (notify == null)
                        {
                            return;
                        }
                        if (DeleteDFAction != null)
                        {
                            DeleteDFAction.Invoke(notify);
                        }
                    }
                    break;
            }
        }

        

        /// <summary>
        /// 查询并获得回执
        /// </summary>
        /// <typeparam name="T1">请求类</typeparam>
        /// <typeparam name="T2">返回类</typeparam>
        /// <param name="req">请求参数</param>
        /// <param name="wait_id">等待这个包回传</param>
        /// <param name="reply">回传</param>
        /// <returns></returns>
        private ErrorDef req_wait_reply<T1, T2>(T1 req, MsgID wait_id, out T2 reply)
        {
            reply = default(T2);
            ErrorDef result_err = ErrorDef.Ok;
            byte[] req_bytes = Codec.Encode(req);
            result_err = write(req_bytes);
            if (result_err != ErrorDef.Ok)
            {
                return result_err;
            }

            //超过这个时间
            int timeout = 3000;
            result_err = ErrorDef.TimeoutErr;
            T2 _reply = default(T2);
            Action<MsgID, string> action = (msg_id, content) =>
            {
                _reply = JsonConvert.DeserializeObject<T2>(content);
                timeout = 0;
                result_err = ErrorDef.Ok;
            };

            WaitPack pack = new WaitPack();
            pack.action = action;
            pack.msg_id = wait_id;

            lock (wait_pack_look)
            {
                wait_pack.AddLast(pack);
            }

            Thread wait_th = new Thread((o) =>
            {
                while (timeout > 0)
                {
                    Thread.Sleep(50);
                    timeout -= 50;
                }
            });
            wait_th.Start();
            wait_th.Join();

            lock (wait_pack_look)
            {
                wait_pack.Remove(pack);
            }
            reply = _reply;

            return result_err;
        }

        /// <summary>
        /// 设置远端设备ip地址
        /// </summary>
        /// <param name="_host"></param>
        public void SetHost(string _host)
        {
            host = _host;
            if (is_connected)
            {
                disconnect();
            }

        }

        /// <summary>
        /// 获得所有防区
        /// </summary>
        /// <param name="offset">偏移</param>
        /// <param name="limit">范围</param>
        /// <param name="start_time">创建时间起始</param>
        /// <param name="finish_time">创建时间终止</param>
        /// <param name="reply">返回查询结果</param>
        public ErrorDef GetDefenceZone(int offset, int limit, DateTime start_time, DateTime finish_time, out GetDefenceZoneReply reply)
        {
            GetDefenceZoneRequest req = new GetDefenceZoneRequest();
            if (start_time != null)
            {
                req.StartTime = start_time.ToString("yyyy-MM-dd HH:mm:ss");
            }
            if (finish_time != null)
            {
                req.FinishTime = finish_time.ToString("yyyy-MM-dd HH:mm:ss");
            }
            req.Offset = offset;
            req.Limit = limit;
            ErrorDef result_err = req_wait_reply(req, MsgID.GetDefenceZoneReplyID, out reply);
            return result_err;
        }


        /// <summary>
        /// 获得历史记录
        /// </summary>
        /// <param name="offset">偏移</param>
        /// <param name="limit">范围</param>
        /// <param name="start_time">创建时间起始</param>
        /// <param name="finish_time">创建时间终止</param>
        /// <param name="reply">返回查询结果</param>
        public ErrorDef GetHistory(int offset, int limit, DateTime start_time, DateTime finish_time, out GetHistoryReply reply)
        {
            GetHistoryRequest req = new GetHistoryRequest();
            if (start_time != null)
            {
                req.StartTime = start_time.ToString("yyyy-MM-dd HH:mm:ss");
            }
            if (finish_time != null)
            {
                req.FinishTime = finish_time.ToString("yyyy-MM-dd HH:mm:ss");
            }
            req.Offset = offset;
            req.Limit = limit;
            ErrorDef result_err = req_wait_reply(req, MsgID.GetHistoryReplyID, out reply);
            return result_err;
        }


        /// <summary>
        /// 布防
        /// </summary>
        /// <param name="zid">防区id</param>
        /// <param name="reply">返回查询结果</param>
        public ErrorDef Deployment(int zid, out DeploymentReply reply)
        {
            DeploymentRequest req = new DeploymentRequest();
            req.ID = zid;

            ErrorDef result_err = req_wait_reply(req, MsgID.DeploymentReplyID, out reply);
            return result_err;
        }

        /// <summary>
        /// 撤防
        /// </summary>
        /// <param name="zid">防区id</param>
        /// <param name="reply">返回查询结果</param>
        public ErrorDef Withdrawal(int zid, out WithdrawalReply reply)
        {
            WithdrawalRequest req = new WithdrawalRequest();
            req.ID = zid;

            ErrorDef result_err = req_wait_reply(req, MsgID.WithdrawalReplyID, out reply);
            return result_err;
        }

        

        /// <summary>
        /// 重置警报
        /// </summary>
        /// <param name="reply">返回查询结果</param>
        public ErrorDef ResetAlarm(out ResetAlarmReply reply)
        {
            ResetAlarmRequest req = new ResetAlarmRequest();

            ErrorDef result_err = req_wait_reply(req, MsgID.ResetAlarmReplyID, out reply);

            return result_err;

        }

        /// <summary>
        /// 消音
        /// </summary>
        /// <param name="reply">返回查询结果</param>
        public ErrorDef CloseAlarmSound(out CloseAlarmSoundReply reply)
        {
            CloseAlarmSoundRequest req = new CloseAlarmSoundRequest();

            ErrorDef result_err = req_wait_reply(req, MsgID.CloseAlarmSoundReplyID, out reply);

            return result_err;

        }
        
    }
}
