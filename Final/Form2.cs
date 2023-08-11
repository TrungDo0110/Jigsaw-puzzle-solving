using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Accord.Math;

namespace Final
{
    public partial class Form2 : Form
    {
        private static HRobot.CallBackFun callback;
        public int device_id = HRobot.Connect("192.168.0.1", 1, callback); //127.0.0.1      169.254.67.4    192.168.0.5

        //global variable for reading puzzle position from txt file
        double[,] txtFile = new double[40, 4];//need to correct when intergrated 2 programs: "1st index_No of pieces +1"

        //global variable for hand-eye calibration
        double[] Homo_extinc = new double[16];
        double[] instrinc = new double[9];
        double[,] extinc1;
        double[,] instrinc5;
        double[,] Homo_extinc1;
        double[] gripper2base1 = new double[16];
        double[,] gripper2base2;
        double[] cam2gripper1 = new double[16];
        double[,] cam2gripper2;

        public Form2()
        {
            InitializeComponent();

            //initialize calibration
            StreamReader ex = new StreamReader("Homo_target2cam.txt");
            for (int i = 0; i < 16; i++)
            {
                Homo_extinc[i] = Convert.ToDouble(ex.ReadLine());
            }
            extinc1 = new double[3, 3] { { Homo_extinc[0], Homo_extinc[1], Homo_extinc[3] },{ Homo_extinc[4], Homo_extinc[5], Homo_extinc[7] },
             { Homo_extinc[8], Homo_extinc[9],Homo_extinc[11]} };

            Homo_extinc1 = new double[4, 4]{ { Homo_extinc[0], Homo_extinc[1],Homo_extinc[2],Homo_extinc[3] },{ Homo_extinc[4], Homo_extinc[5],Homo_extinc[6] ,Homo_extinc[7] },
             { Homo_extinc[8], Homo_extinc[9],Homo_extinc[10],Homo_extinc[11]}, { Homo_extinc[12], Homo_extinc[13],Homo_extinc[14],Homo_extinc[15]}};

            StreamReader instr = new StreamReader("instrinc.txt");
            for (int i = 0; i < 9; i++)
            {
                instrinc[i] = Convert.ToDouble(instr.ReadLine());
            }
            instrinc5 = new double[3, 3] { { instrinc[0], Homo_extinc[1],instrinc[2]}, { instrinc[3], instrinc[4], instrinc[5] },
             {instrinc[6], instrinc[7],instrinc[8] }};


            StreamReader gripper2base = new StreamReader("Homo_gripper2base.txt");
            for (int i = 0; i < 16; i++)
            {
                gripper2base1[i] = Convert.ToDouble(gripper2base.ReadLine());
            }
            gripper2base2 = new double[4, 4]{ { gripper2base1[0], gripper2base1[1],gripper2base1[2],gripper2base1[3] },{ gripper2base1[4],gripper2base1[5],gripper2base1[6] ,gripper2base1[7] },
             { gripper2base1[8], gripper2base1[9],gripper2base1[10],gripper2base1[11]}, { gripper2base1[12],gripper2base1[13],gripper2base1[14],gripper2base1[15]}};

            StreamReader cam2gripper = new StreamReader("Homo_cam2gripper.txt");
            for (int i = 0; i < 16; i++)
            {
                cam2gripper1[i] = Convert.ToDouble(cam2gripper.ReadLine());
            }
            cam2gripper2 = new double[4, 4]{ { cam2gripper1[0], cam2gripper1[1],cam2gripper1[2],cam2gripper1[3] },{ cam2gripper1[4],cam2gripper1[5],cam2gripper1[6] ,cam2gripper1[7] },
             { cam2gripper1[8], cam2gripper1[9],cam2gripper1[10],cam2gripper1[11]}, { cam2gripper1[12],cam2gripper1[13],cam2gripper1[14],cam2gripper1[15]}};
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            label2.Text = device_id.ToString();
            if (device_id >= 0)
            {
                HRobot.set_motor_state(device_id, 1);
                HRobot.set_override_ratio(device_id, 20);
                label1.Text = ("connect successful.");
                int[] tool_base = new int[2];
            }
            else
            {
                Console.WriteLine("connect failure.");
                label1.Text = "connect failure.";
            }
            FileStream fs = new FileStream(@"0test.txt", FileMode.Open);
            StreamReader rd = new StreamReader(fs, Encoding.UTF8);
            string input = rd.ReadToEnd();
            int i = 0, j = 0;

            foreach (var row in input.Split('\n'))
            {
                j = 0;
                foreach (var col in row.Trim().Split(' '))
                {
                    string val = col.Trim();
                    double.TryParse(val, out txtFile[i, j]);
                    j++;
                }
                i++;
            }
        }

        //private void button1_Click(object sender, EventArgs e)
        //{
        //    timer2.Enabled = !timer2.Enabled;

        //}
        //private void timer2_Tick(object sender, EventArgs e)
        //{
        //    double[] axi = new double[6];
        //    double[] cur_poss = new double[6];
        //    HRobot.get_base_data(device_id, 5, axi);
        //    HRobot.get_current_position(device_id, cur_poss);
        //    label3.Text = axi[0].ToString();
        //    label9.Text = cur_poss[0].ToString();
        //    label10.Text = cur_poss[1].ToString();
        //    label11.Text = cur_poss[2].ToString();
        //    label12.Text = cur_poss[3].ToString();
        //    label13.Text = cur_poss[4].ToString();
        //    label14.Text = cur_poss[5].ToString();

        //}

        private void timer1_Tick(object sender, EventArgs e)
        {
            double[] axi = new double[6];
            double[] cur_poss = new double[6];
            HRobot.get_base_data(device_id, 5, axi);
            HRobot.get_current_position(device_id, cur_poss);
            label3.Text = axi[0].ToString();
            label9.Text = cur_poss[0].ToString();
            label10.Text = cur_poss[1].ToString();
            label11.Text = cur_poss[2].ToString();
            label12.Text = cur_poss[3].ToString();
            label13.Text = cur_poss[4].ToString();
            label14.Text = cur_poss[5].ToString();

            double[] rel_poss = new double[6];
            HRobot.get_current_joint(device_id, rel_poss);
            label21.Text = rel_poss[0].ToString();
            label22.Text = rel_poss[1].ToString();
            label23.Text = rel_poss[2].ToString();
            label24.Text = rel_poss[3].ToString();
            label25.Text = rel_poss[4].ToString();
            label26.Text = rel_poss[5].ToString();

            double[] tool_axi = new double[6];
            HRobot.get_tool_data(device_id, 10, tool_axi);
            label34.Text = tool_axi[0].ToString();
            label35.Text = tool_axi[1].ToString();
            label36.Text = tool_axi[2].ToString();
            label37.Text = tool_axi[3].ToString();
            label38.Text = tool_axi[4].ToString();
            label39.Text = tool_axi[5].ToString();

            label46.Text = HRobot.get_DO(device_id, 9).ToString();
        }

        //Move to desired point
        private void MoveToPosition_Click(object sender, EventArgs e)
        {
            HRobot.set_override_ratio(device_id, 50);
            double[] pos = new double[6];
            pos[0] = Convert.ToDouble(textBox1.Text);
            pos[1] = Convert.ToDouble(textBox2.Text);
            pos[2] = Convert.ToDouble(textBox3.Text);
            pos[3] = Convert.ToDouble(textBox6.Text);
            pos[4] = Convert.ToDouble(textBox5.Text);
            pos[5] = Convert.ToDouble(textBox4.Text);
            HRobot.ptp_pos(device_id, 0, pos);
        }
        private void MoveToAngle_Click(object sender, EventArgs e)
        {
            HRobot.set_override_ratio(device_id, 50);
            double[] pos = new double[6];
            pos[0] = Convert.ToDouble(textBox13.Text);
            pos[1] = Convert.ToDouble(textBox12.Text);
            pos[2] = Convert.ToDouble(textBox11.Text);
            pos[3] = Convert.ToDouble(textBox10.Text);
            pos[4] = Convert.ToDouble(textBox9.Text);
            pos[5] = Convert.ToDouble(textBox8.Text);
            HRobot.ptp_axis(device_id, 0, pos);
        }

        //Setup Valve by hand
        private void ValveOn_Click(object sender, EventArgs e)
        {
            HRobot.set_DO(device_id, 9, true);
        }
        private void ValveOff_Click(object sender, EventArgs e)
        {
            HRobot.set_DO(device_id, 9, false);
        }


        #region Move by hand
        private void MoveXp_Click(object sender, EventArgs e)
        {
            double[] pos1 = { 10, 0, 0, 0, 0, 0 };
            HRobot.ptp_rel_pos(device_id, 0, pos1);
        }
        private void MoveXn_Click(object sender, EventArgs e)
        {
            double[] pos2 = { -10, 0, 0, 0, 0, 0 };
            HRobot.ptp_rel_pos(device_id, 0, pos2);
        }
        private void MoveYp_Click(object sender, EventArgs e)
        {
            double[] pos1 = { 0, 10, 0, 0, 0, 0 };
            HRobot.ptp_rel_pos(device_id, 0, pos1);
        }
        private void MoveYn_Click(object sender, EventArgs e)
        {
            double[] pos2 = { 0, -10, 0, 0, 0, 0 };
            HRobot.ptp_rel_pos(device_id, 0, pos2);
        }
        private void MoveZp_Click(object sender, EventArgs e)
        {
            double[] pos1 = { 0, 0, 10, 0, 0, 0 };
            HRobot.ptp_rel_pos(device_id, 0, pos1);
        }
        private void MoveZn_Click(object sender, EventArgs e)
        {
            double[] pos2 = { 0, 0, -10, 0, 0, 0 };
            HRobot.ptp_rel_pos(device_id, 0, pos2);
        }
        private void MoveAp_Click(object sender, EventArgs e)
        {
            double[] pos1 = { 0, 0, 0, 10, 0, 0 };
            HRobot.ptp_rel_pos(device_id, 0, pos1);
        }
        private void MoveAn_Click(object sender, EventArgs e)
        {
            double[] pos2 = { 0, 0, 0, -10, 0, 0 };
            HRobot.ptp_rel_pos(device_id, 0, pos2);
        }
        private void MoveBp_Click(object sender, EventArgs e)
        {
            double[] pos1 = { 0, 0, 0, 0, 10, 0 };
            HRobot.ptp_rel_pos(device_id, 0, pos1);
        }
        private void MoveBn_Click(object sender, EventArgs e)
        {
            double[] pos2 = { 0, 0, 0, 0, -10, 0 };
            HRobot.ptp_rel_pos(device_id, 0, pos2);
        }
        private void MoveCp_Click(object sender, EventArgs e)
        {
            double[] pos1 = { 0, 0, 0, 0, 0, 10 };
            HRobot.ptp_rel_pos(device_id, 0, pos1);
        }
        private void MoveCn_Click(object sender, EventArgs e)
        {
            double[] pos2 = { 0, 0, 0, 0, 0, -10 };
            HRobot.ptp_rel_pos(device_id, 0, pos2);
        }
        #endregion


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            HRobot.set_motor_state(device_id, 0);
        }

        //Move according to the Image processing
        private void Excute_Click(object sender, EventArgs e)
        {
            //HRobot.set_override_ratio(device_id, 50);
            //int pieQuan = Convert.ToInt32(textBox7.Text);
            //for (int i = 0; i < pieQuan; i++)
            //{
            //    HRobot.set_DO(device_id, 9, false);
            //    double X, Y;
            //    CorrectPiecesPos.Pos(txtFile[i, 0], out X, out Y);
            //    double angle = -txtFile[i, 1];
            //    double[] pos1 = { txtFile[i, 2], txtFile[i, 3], -50, 0, 0, -90 };
            //    double[] pos2 = { txtFile[i, 2], txtFile[i, 3], 5, 0, 0, -90 };
            //    double[] pos3 = { X, Y, -50, 0, 0, angle };
            //    double[] pos4 = { X, Y, -10, 0, 0, angle };
            //    HRobot.ptp_pos(device_id, 0, pos1);
            //    HRobot.ptp_pos(device_id, 0, pos2);
            //    robot_wait();
            //    Thread.Sleep(1000);
            //    HRobot.set_DO(device_id, 9, true);
            //    Thread.Sleep(1000);
            //    HRobot.ptp_pos(device_id, 0, pos1);
            //    HRobot.ptp_pos(device_id, 0, pos3);
            //    HRobot.ptp_pos(device_id, 0, pos4);
            //    robot_wait();
            //    Thread.Sleep(1000);
            //    HRobot.set_DO(device_id, 9, false);
            //    Thread.Sleep(1000);
            //    HRobot.ptp_pos(device_id, 0, pos3);
            HRobot.set_override_ratio(device_id, 50);
            int pieQuan = Convert.ToInt32(textBox7.Text);
            for (int ii = 0; ii < pieQuan; ii++)
            {
                txtFile[ii, 2] = 3120 - txtFile[ii, 2];
                txtFile[ii, 3] = 2164 - txtFile[ii, 3];
                double[,] pixel = new double[3, 1] { { txtFile[ii, 2] }, { txtFile[ii, 3] }, { 1 } };
                double[,] world = new double[4, 1];
                double[,] c;
                double[,] B;
                double[,] D;
                double[,] E;
                double[,] F;
                c = Matrix.Multiply(extinc1.Inverse(), instrinc5.Inverse());
                B = Matrix.Multiply(c, pixel);
                for (int k = 0; k < 4; k++)
                {
                    if (k < 2)
                        world[k, 0] = B[k, 0] / B[2, 0];
                    else if (k == 2)
                        world[k, 0] = 0;
                    else if (k > 2)
                        world[k, 0] = B[k - 1, 0] / B[2, 0];
                }

                D = Matrix.Multiply(gripper2base2, cam2gripper2);
                E = Matrix.Multiply(D, Homo_extinc1);
                F = Matrix.Multiply(E, world);
                txtFile[ii, 2] = F[0, 0];
                txtFile[ii, 3] = F[1, 0];
                //MessageBox.Show(F[0, 0].ToString());
                //MessageBox.Show(F[1, 0].ToString());

                HRobot.set_DO(device_id, 9, false);
                double X, Y;
                CorrectPiecesPos.Pos(txtFile[ii, 0], out X, out Y);
                double angle = txtFile[ii, 1] + 90;
                double[] pos1 = { txtFile[ii, 2], txtFile[ii, 3], 50, -180, 0, 90 };
                double[] pos2 = { txtFile[ii, 2], txtFile[ii, 3], -2, -180, 0, 90 };
                //double[] pos3 = { X, Y, 50, -180, 0, angle };
                //double[] pos4 = { X, Y, 2, -180, 0, angle };
                double[] pos3 = { X, Y, 50, -180, 0, angle };
                double[] pos4 = { 0, 0, -45, 0, 0, 0 };
                double[] pos5 = { 0, 0, 45, 0, 0, 0 };

                //double[] pos_loc = { 311.846, 538.379, 50, -180, 0, angle };
                HRobot.ptp_pos(device_id, 0, pos1);
                HRobot.ptp_pos(device_id, 0, pos2);
                robot_wait();
                Thread.Sleep(1000);
                HRobot.set_DO(device_id, 9, true);
                Thread.Sleep(1000);
                HRobot.ptp_pos(device_id, 0, pos1);

                //HRobot.ptp_pos(device_id, 0, pos_loc);//定位
                HRobot.ptp_pos(device_id, 0, pos3);//相對
                HRobot.ptp_rel_pos(device_id, 0, pos4);//相對
                robot_wait();
                Thread.Sleep(1000);
                HRobot.set_DO(device_id, 9, false);
                Thread.Sleep(1000);
                HRobot.ptp_rel_pos(device_id, 0, pos5);//相對
                //HRobot.ptp_pos(device_id, 0, pos3);
            }

        }

        private void robot_wait()
        {
            Thread.Sleep(100);
            while (HRobot.get_motion_state(device_id) == 2)
            {
                Thread.Sleep(10);
            }
            Thread.Sleep(10);
        }

        //Move to the camera point
        private void CameraPts_Click(object sender, EventArgs e)
        {
            HRobot.set_override_ratio(device_id, 50);
            double[] pos = new double[6];
            pos[0] = 0;
            pos[1] = 0;
            pos[2] = 0;
            pos[3] = 0;
            pos[4] = -90;
            pos[5] = 0;
            HRobot.ptp_axis(device_id, 0, pos);
            //int speed = HRobot.get_override_ratio(device_id);
            //Console.WriteLine(speed);
        }
    }
}
