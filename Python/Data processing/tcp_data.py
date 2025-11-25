import socket
import time
import numpy as np
import struct
import mysql.connector
import pandas as pd
from datetime import datetime
from sqlalchemy import create_engine

message_1 = bytes.fromhex('01 03 00 20 00 10 45 CC')
message_2 =bytes.fromhex('05 04 00 00 00 02 70 4F')
message_3 =bytes.fromhex('04 04 00 00 00 02 71 9E')
message_4=bytes.fromhex('01 04 00 00 00 02 71 CB')
message_5 =bytes.fromhex('03 04 00 00 00 02 70 29')
message_6 =bytes.fromhex('02 04 00 00 00 02 71 F8')

conn = mysql.connector.connect(host='localhost', user='root', password='1312699832', database='data2025')
table=str(datetime.now().strftime("%Y%m%d"))+'''_dahua'''
cursor = conn.cursor()
create_table_query = '''
create table IF NOT EXISTS '''+table+'''(
passed_time int,
tem_1 float(5,2),
tem_2 float(5,2),
tem_3 float(5,2),
tem_4 float(5,2),
tem_5 float(5,2),
tem_6 float(5,2),
dis_1 float(6,4),
dis_2 float(6,4),
dis_3 float(6,4),
dis_4 float(6,4),
dis_5 float(6,4)
);
'''
cursor.execute(create_table_query)
print("[SUCCESS] 成功创建数据表："+table)
cursor.close()
conn.close()

class LongConnectionClient:
    def __init__(self, server_host='192.168.200.20', server_port_1=1030,server_port_2=1031,server_port_3=1032,server_port_4=1033,server_port_5=1034,server_port_6=1035):
        self.server_host = server_host
        self.server_port_1 = server_port_1
        self.server_port_2 = server_port_2
        self.server_port_3 = server_port_3
        self.server_port_4 = server_port_4
        self.server_port_5 = server_port_5
        self.server_port_6 = server_port_6
        self.sock_1 =None
        self.sock_2 =None
        self.sock_3 =None
        self.sock_4 =None
        self.sock_5 =None
        self.sock_6 =None

    def connect(self):
        try:
            self.sock_1 = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            self.sock_2 = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            self.sock_3 = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            self.sock_4 = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            self.sock_5 = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            self.sock_6 = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            self.sock_1.connect((self.server_host, self.server_port_1))
            self.sock_2.connect((self.server_host, self.server_port_2))
            self.sock_3.connect((self.server_host, self.server_port_3))
            self.sock_4.connect((self.server_host, self.server_port_4))
            self.sock_5.connect((self.server_host, self.server_port_5))
            self.sock_6.connect((self.server_host, self.server_port_6))
            self.sock_1.settimeout(10)
            self.sock_2.settimeout(10)
            self.sock_3.settimeout(10)
            self.sock_4.settimeout(10)
            self.sock_5.settimeout(10)
            self.sock_6.settimeout(10)
            print("[SUCCESS] 连接客户端成功")
            return True
        except Exception as e:
            print(f"[ERROR] 连接失败: {str(e)}")
            return False

    def send_data(self, data_1, data_2, data_3, data_4, data_5, data_6):
        """发送数据"""
        try:
            for i in range(0,10):
                self.sock_1.send(data_1)
                self.sock_2.send(data_2)
                self.sock_3.send(data_3)
                self.sock_4.send(data_4)
                self.sock_5.send(data_5)
                self.sock_6.send(data_6)
                time.sleep(0.996)
                response_1 = self.sock_1.recv(37)
                response_2 = self.sock_2.recv(9)
                response_3 = self.sock_3.recv(9)
                response_4 = self.sock_4.recv(9)
                response_5 = self.sock_5.recv(9)
                response_6 = self.sock_6.recv(9)

                tem = np.array(
                    [int.from_bytes([response_1[i:i + 2] for i in range(3, 15, 2)][i], byteorder='big') for i in
                     range(0, 6, 1)]) / 10
                dis_1 = "{:.4f}".format(struct.unpack('>f', (response_2[3:7]))[0])
                dis_2 = "{:.4f}".format(struct.unpack('>f', (response_3[3:7]))[0])
                dis_3 = "{:.4f}".format(struct.unpack('>f', (response_4[3:7]))[0])
                dis_4 = "{:.4f}".format(struct.unpack('>f', (response_5[3:7]))[0])
                dis_5 = "{:.4f}".format(struct.unpack('>f', (response_6[3:7]))[0])
                temdis=np.array([tem[0],tem[1],tem[2],tem[3],tem[4],tem[5],dis_1, dis_2, dis_3, dis_4, dis_5])
                # print(temdis)
                if i == 0:
                    comb = temdis
                else:
                    comb = np.vstack((comb, temdis))
            engine = create_engine('mysql://root:1312699832@localhost/data2025')
            final_data = pd.DataFrame(comb)
            try:
                final_data.to_sql(table, engine, if_exists='append', index=False)
            except Exception as e:
                final_data.to_sql(table, engine, if_exists='replace', index=False)
        except Exception as e:
            print(f"[ERROR] 数据传输失败: {str(e)}")
            self.reconnect()


    def reconnect(self):
        """断线重连"""
        if self.sock_1 or self.sock_2 or self.sock_3 or self.sock_4 or self.sock_5 or self.sock_6:
            self.sock_1.close()
            self.sock_2.close()
            self.sock_3.close()
            self.sock_4.close()
            self.sock_5.close()
            self.sock_6.close()
        print("[*] 尝试重新连接...")
        while not self.connect():
            time.sleep(5)

    def run(self):
        if not self.connect():
            return
        try:
            while True:
                self.send_data(message_1,message_2,message_3,message_4,message_5,message_6)
        except KeyboardInterrupt:
            self.sock_1.close()
            self.sock_2.close()
            self.sock_3.close()
            self.sock_4.close()
            self.sock_5.close()
            self.sock_6.close()

if __name__ == '__main__':
    client = LongConnectionClient()
    client.run()