using System;
using System.Windows.Forms;

namespace CoreControllers
{
    public partial class ByteMask : UserControl
    {
        public ByteMask()
        {
            InitializeComponent();
        }

        private void ByteMask_Resize(object sender, EventArgs e)
        {
            Width = 146;
            Height = 68;
        }

        private void b0_CheckedChanged(object sender, EventArgs e)
        {
            CalcValue();
        }

        private void b1_CheckedChanged(object sender, EventArgs e)
        {
            CalcValue();
        }

        private void b2_CheckedChanged(object sender, EventArgs e)
        {
            CalcValue();
        }

        private void b3_CheckedChanged(object sender, EventArgs e)
        {
            CalcValue();
        }

        private void b4_CheckedChanged(object sender, EventArgs e)
        {
            CalcValue();
        }

        private void b5_CheckedChanged(object sender, EventArgs e)
        {
            CalcValue();
        }

        private void b6_CheckedChanged(object sender, EventArgs e)
        {
            CalcValue();
        }

        private void b7_CheckedChanged(object sender, EventArgs e)
        {
            CalcValue();
        }

        private int CurrentValue = 0;

        private void CalcValue()
        {
            CurrentValue = 0;
            string binary = "";


            if (b0.Checked)
            {
                CurrentValue += 1;
                binary = "1" + binary;
            }
            else
            {
                binary = "0" + binary;
            }


            if (b1.Checked)
            {
                CurrentValue += 2;
                binary = "1" + binary;
            }
            else
            {
                binary = "0" + binary;
            }


            if (b2.Checked)
            {
                CurrentValue += 4;
                binary = "1" + binary;
            }
            else
            {
                binary = "0" + binary;
            }


            if (b3.Checked)
            {
                CurrentValue += 8;
                binary = "1" + binary;
            }
            else
            {
                binary = "0" + binary;
            }


            if (b4.Checked)
            {
                CurrentValue += 16;
                binary = "1" + binary;
            }
            else
            {
                binary = "0" + binary;
            }


            if (b5.Checked)
            {
                CurrentValue += 32;
                binary = "1" + binary;
            }
            else
            {
                binary = "0" + binary;
            }


            if (b6.Checked)
            {
                CurrentValue += 64;
                binary = "1" + binary;
            }
            else
            {
                binary = "0" + binary;
            }


            if (b7.Checked)
            {
                CurrentValue += 128;
                binary = "1" + binary;
            }
            else
            {
                binary = "0" + binary;
            }

            label1.Text = "Двоичное: " + binary;
            label2.Text = "Десятичное: " + CurrentValue.ToString();
        }


        public void SetNewValue(int _value)
        {
            string[] ss = byteToBitsString((byte) _value);

            b0.Checked = (ss[0] == "1");
            b1.Checked = (ss[1] == "1");
            b2.Checked = (ss[2] == "1");
            b3.Checked = (ss[3] == "1");
            b4.Checked = (ss[4] == "1");
            b5.Checked = (ss[5] == "1");
            b6.Checked = (ss[6] == "1");
            b7.Checked = (ss[7] == "1");
        }

        public int GetValue()
        {
            return CurrentValue;
        }

        public string[] GetBooleanValue()
        {
            return byteToBitsString((byte)CurrentValue);
        }

        private string[] byteToBitsString(byte byteIn)
        {
            string[] bits = new String[8];
            bits[7] = Convert.ToString((byteIn / 128) % 2);
            bits[6] = Convert.ToString((byteIn / 64) % 2);
            bits[5] = Convert.ToString((byteIn / 32) % 2);
            bits[4] = Convert.ToString((byteIn / 16) % 2);
            bits[3] = Convert.ToString((byteIn / 8) % 2);
            bits[2] = Convert.ToString((byteIn / 4) % 2);
            bits[1] = Convert.ToString((byteIn / 2) % 2);
            bits[0] = Convert.ToString((byteIn / 1) % 2);
            return bits;
        }
    }
}
