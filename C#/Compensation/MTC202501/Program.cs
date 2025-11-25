using Sdcb.OpenVINO;
using System.Numerics;

namespace MtcLibrary1;

public static class Program
{
    static void Main()
    {
        string work = "compensation";

        if (work == "compensation")
        {
            Console.WriteLine("10.18");
            int BatchSize = 32;
            Infer ov_infer = new Infer();
            string password = ;
            FileEncryption fileEncryption = new FileEncryption(password);
            float[] first_tem = ov_infer.Get_temper(1);
            float[] test_tem = first_tem;
            for (int i = 0; i < 6; i++)
            {
                if (test_tem[i] < -10 || test_tem[i] > 50)
                {
                    Console.WriteLine("[Error]:PT100 warning");
                    return;
                }
            }
            int[] compen_value_storge = { 0, 0, 0, 0, 0, 0 };
            float[] para = { 15.0f, 0.2f, 0.08f, 0.45f, 3.7f, 3.4f, 5.2f };//tem_one, tem_b, dis_one, dis_b, pxx, pyx, pz
            while (true)
            {
                test_tem = ov_infer.Get_temper(1);
                for (int i = 0; i < 6; i++)
                {
                    if (test_tem[i] < -10 || test_tem[i] > 50)
                    {
                        Console.WriteLine("[Error]:PT100 warning");
                        return;
                    }
                }
                ov_infer.Run_infer(para, ov_infer.Get_temper(BatchSize), BatchSize, first_tem, compen_value_storge, out compen_value_storge);
                fileEncryption.DeleteFile();
            }
        }