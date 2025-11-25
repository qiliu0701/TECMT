using Sdcb.OpenVINO;
using System.Diagnostics;
using EZSockets;
using System.Security.Cryptography;
using System.Text;
using System.Net.Sockets;
using RJCP.IO.Ports;
using System;
using System.Xml;
using System.Numerics;

namespace MtcLibrary1
{
    public class Infer
    {
        public float[] Get_temper(int bs)
        {
            byte[] msg = new byte[] { 0x01, 0x03, 0x00, 0x20, 0x00, 0x10, 0x45, 0xCC };
            List<List<float>> inputDatas = new List<List<float>>();
            List<float> inputData = new List<float>();
            using (SerialPortStream ser = new SerialPortStream("COM4", 9600))
            {
                ser.Open();
                for (int i = 0; i < bs; i++)
                {
                    ser.Write(msg, 0, msg.Length);
                    Thread.Sleep(9994);
                    byte[] returnedData = new byte[37];
                    ser.Read(returnedData, 0, 37);
                    for (int j = 3; j < 15; j += 2) // 3-14共6组数据
                    {
                        // 处理字节序转换（大端转系统字节序）
                        byte[] buffer = new byte[2];
                        if (BitConverter.IsLittleEndian)
                        {
                            buffer[0] = returnedData[j + 1];
                            buffer[1] = returnedData[j];
                        }
                        else
                        {
                            buffer[0] = returnedData[j];
                            buffer[1] = returnedData[j + 1];
                        }
                        short value = BitConverter.ToInt16(buffer, 0);
                        inputData.Add((float)(value / 10.0));
                    }
                    inputDatas.Add(inputData);
                    inputData = new List<float>();
                }
                ser.Close();
                int count = inputDatas.Count * inputDatas[0].Count;
                float[] reshaped = new float[count];
                for (int i = 0; i < inputDatas.Count; i++)
                {
                    for (int j = 0; j < inputDatas[0].Count; j++)
                    {
                        reshaped[i * 6 + j] = inputDatas[i][j];
                    }
                }
                string filePath = "C:\\Users\\Admin\\Desktop\\" + DateTime.Now.ToString("MM月dd日") + "_温度值记录.txt";
                string timeStamp = DateTime.Now.ToString("HH:mm:ss");
                string logMessage = $"[本次温度: [{string.Join(", ", reshaped)}]\n\n";
                File.AppendAllText(filePath, logMessage);
                return reshaped;
            }
        }
        public void Run_infer(float[] para, float[] Final_tem, int bs, float[] first_tem, int[] last_compen, out int[] this_compen)
        {
            ReadOnlySpan<float> data;
            string password = ;
            string ip = "192.168.200.1";
            FileEncryption fileEncryption = new FileEncryption(password);
            Compen compen = new Compen();

            float[] processed_tem = new float[bs * 6];
            for (int i = 0; i < bs; i++)
            {
                for (int j = 0; j < 6; j++) 
                {
                    processed_tem[i * 6 + j] = (Final_tem[i * 6 + j] - first_tem[j]) / para[0] + para[1];
                }
            }
            fileEncryption.DecryptFile();
            using Model rawModelz = OVCore.Shared.ReadModel("");
            using CompiledModel cmz = OVCore.Shared.CompileModel(rawModelz, "CPU");
            using InferRequest irz = cmz.CreateInferRequest();
            long[] a = { 1, bs, 6 };
            Shape inputShape = new Shape(a);
            using (Tensor input = Tensor.FromArray<float>(processed_tem, inputShape)) { irz.Inputs.Primary = input; }
            Stopwatch stopwatch = new();
            stopwatch.Start();
            irz.Run();
            stopwatch.Stop();
            double inferTime = stopwatch.Elapsed.TotalMilliseconds;

            using (Tensor outputz = irz.Outputs.Primary)
            {
                data = outputz.GetData<float>();
                float z = -(data[0] - para[3]) * para[2] / para[6] * 5 * 1000; //z
                float y = (data[4] - para[3]) * para[2] / para[5] * 5 * 1000* 1.25f; //yx
                float x = -(data[1] - para[3]) * para[2] / para[4] * 5 * 1000 ; //xx
                float[] compen_xyz = new float[] { x, y, z };
                Console.WriteLine($"infer time: {inferTime:F5}ms");
                this_compen = compen.GetDeviceWrite(compen_xyz, ip, last_compen);
            }

        }
    }
    public class Compen
    {
        private EZNCAUTLib.DispEZNcCommunication EZNcCom;//通讯库变量
        private int lResult = 1;
        private int lSystemType;
        public void GetSimConnect1(string machine_ip)
        {
            lSystemType = (int)sysType.EZNC_SYS_MELDAS800M;
            if (EZNcCom == null)
            {
                string ip = machine_ip;
                EZNcCom = new EZNCAUTLib.DispEZNcCommunication();
                lResult = EZNcCom.SetTCPIPProtocol(ip, 683);//683
                lResult = EZNcCom.Open2(lSystemType, 1, 1, "EZNC_LOCALHOST");//EZNC_LOCALHOST
            }
        }
        public int[] GetDeviceWrite(float[] compen_xyz, string ip, int[] last_compen)
        {
            GetSimConnect1(ip);
            lResult = 0;
            if (lResult != 0) { Console.WriteLine("[Error]:Socket warning"); return last_compen; }
            int[] compen = { 0, 0, 0, 0, 0, 0 };
            compen[0] = Convert.ToInt32(compen_xyz[0]);
            if (compen[0] < 0) { compen[1] = -1; }
            compen[2] = Convert.ToInt32(compen_xyz[1]);
            if (compen[2] < 0) { compen[3] = -1; }
            compen[4] = Convert.ToInt32(compen_xyz[2]);
            if (compen[4] < 0) { compen[5] = -1; }
            if ( Math.Abs(compen[0]) < 20 && Math.Abs(compen[2]) < 20 && Math.Abs(compen[4]) < 25)
            //if (Math.Abs(compen[0] - last_compen[0]) < 3 && Math.Abs(compen[2] - last_compen[2]) < 3 && Math.Abs(compen[4] - last_compen[4]) < 3 && Math.Abs(compen[0]) < 20 && Math.Abs(compen[2]) < 20 && Math.Abs(compen[4]) < 25)
                {
                lResult = EZNcCom.Device_WriteBlock(6, "R5700", 20, compen);
                if (lResult == 0)
                {
                    string filePath = "C:\\Users\\Admin\\Desktop\\" + DateTime.Now.ToString("MM月dd日") + "_正常补偿值记录.txt";
                    string timeStamp = DateTime.Now.ToString("HH:mm:ss");
                    string logMessage = $"[本次位移: [{string.Join(", ", compen)}]\n\n";
                    File.AppendAllText(filePath, logMessage);
                    Console.Write("Compensation: ");
                    Console.Write($"[x]:{compen[0]} ");
                    Console.Write($"[y]:{compen[2]} ");
                    Console.WriteLine($"[z]:{compen[4]}");
                    return compen;
                }
                else
                {
                    Console.WriteLine("[Error]:Device Write Warning");
                    return last_compen;
                }
            }
            else
            {

                string filePath = "C:\\Users\\Admin\\Desktop\\" + DateTime.Now.ToString("MM月dd日") + "补偿值异常日志.txt";
                string timeStamp = DateTime.Now.ToString("HH:mm:ss");
                string logMessage = $"[{timeStamp}] 上次位移: [{string.Join(", ", last_compen)}] 本次位移: [{string.Join(", ", compen)}]\n\n";
                File.AppendAllText(filePath, logMessage);
                Console.WriteLine("[ERROR]:补偿值异常，具体情况请查看日志");
                return last_compen;
            }

        }
    }


