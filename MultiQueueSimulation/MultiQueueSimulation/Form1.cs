using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MultiQueueModels;
using MultiQueueTesting;

namespace MultiQueueSimulation
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            comboBox1.Items.Add("Priority");
            comboBox1.Items.Add("Random");
            comboBox1.Items.Add("Least utilization");

            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox1.SelectedIndex = 0;
            comboBox2.Items.Add("Maximum Number of customers");
            comboBox2.Items.Add("Simulation end time");
            comboBox2.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox2.SelectedIndex = 0;
        }

        private void label1_Click(object sender, EventArgs e)
        {
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            // جدول الـ Interarrival
            dataGridView1.Columns.Add("InterarrivalTime", "Interarrival Time");
            dataGridView1.Columns.Add("Probability", "Probability");
            InitializeMainTable(5);
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;

            panel1.AutoScroll = true;

            // جدول النتائج
            dataGridViewResults.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewResults.AllowUserToAddRows = false;
            dataGridViewResults.AllowUserToDeleteRows = false;

            // إنشاء الأعمدة المطلوبة لعرض كل القيم
            dataGridViewResults.Columns.Add("CustomerNo", "Customer No");
            dataGridViewResults.Columns.Add("RandInterArrival", "Random Digit for Inter-Arrival");
            dataGridViewResults.Columns.Add("InterArrival", "Inter-Arrival Time");
            dataGridViewResults.Columns.Add("ArrivalTime", "Arrival Time (Clock Time)");
            dataGridViewResults.Columns.Add("RandService", "Random Digit for Service Duration");
            dataGridViewResults.Columns.Add("ServiceTime", "Service Duration");
            dataGridViewResults.Columns.Add("ServerIndex", "Server Index");
            dataGridViewResults.Columns.Add("StartTime", "Time Service Begins");
            dataGridViewResults.Columns.Add("EndTime", "Time Service Ends (Departure)");
            dataGridViewResults.Columns.Add("TimeInQueue", "Total Delay (Time in Queue)");
        }


        private void InitializeMainTable(int numRows)
        {
            dataGridView1.Rows.Clear();
            for (int i = 0; i < numRows; i++)
                dataGridView1.Rows.Add();
        }
        private void label5_Click(object sender, EventArgs e)
        {
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void label2_Click(object sender, EventArgs e)
        {
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            UpdateSubTables();

        }
        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            int numRows;
            if (int.TryParse(textBox3.Text, out numRows) && numRows > 0)
            {
                InitializeMainTable(numRows);
            }
            else
            {
                numRows = 5;
                InitializeMainTable(numRows);
            }
            UpdateSubTables();
        }

        private void UpdateSubTables()
        {
            int numTables;
            int numRows = dataGridView1.Rows.Count > 0 ? dataGridView1.Rows.Count : 5;

            if (int.TryParse(textBox2.Text, out numTables) && numTables > 0)
            {
                panel1.Controls.Clear();

                int xOffset = 10;
                int yOffset = 10;

                for (int i = 0; i < numTables; i++)
                {
                    Label lbl = new Label();
                    lbl.Text = $"Server {i + 1}";
                    lbl.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                    lbl.AutoSize = true;
                    lbl.Left = xOffset + 30;
                    lbl.Top = yOffset;

                    Panel subPanel = new Panel();
                    subPanel.Left = xOffset;
                    subPanel.Top = lbl.Bottom + 5;
                    subPanel.BackColor = Color.White;
                    subPanel.BorderStyle = BorderStyle.FixedSingle;

                    DataGridView dgv = new DataGridView();
                    dgv.Name = $"serverTable{i + 1}";
                    dgv.Width = dataGridView1.Width;
                    dgv.AutoSizeColumnsMode = dataGridView1.AutoSizeColumnsMode;
                    dgv.AllowUserToAddRows = false;
                    dgv.AllowUserToDeleteRows = false;
                    dgv.RowHeadersVisible = false;

                    dgv.Columns.Add("ServiceTime", "Service Time");
                    dgv.Columns.Add("Probability", "Probability");

                    for (int r = 0; r < numRows; r++)
                        dgv.Rows.Add();

                    int headerHeight = dgv.ColumnHeadersHeight;
                    int rowsHeight = dgv.RowTemplate.Height * numRows;
                    dgv.Height = headerHeight + rowsHeight + 2;

                    subPanel.Width = dgv.Width + 2;
                    subPanel.Height = dgv.Height + 2;
                    subPanel.Controls.Add(dgv);

                    panel1.Controls.Add(lbl);
                    panel1.Controls.Add(subPanel);

                    xOffset += subPanel.Width + 20;
                }
            }
            else
            {
                panel1.Controls.Clear();
            }
        }

        private void btnRunSimulation_Click(object sender, EventArgs e)
        {
            try
            {
                SimulationSystem system = new SimulationSystem();

                // اختيار طريقة اختيار السيرفر
                string selectedMethod = comboBox1.SelectedItem.ToString();
                if (selectedMethod == "Random")
                    system.SelectionMethod = Enums.SelectionMethod.Random;
                else if (selectedMethod == "Priority")
                    system.SelectionMethod = Enums.SelectionMethod.HighestPriority;
                else
                    system.SelectionMethod = Enums.SelectionMethod.LeastUtilization;

                // قراءة جدول الـ Interarrival
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (row.Cells[0].Value != null && row.Cells[1].Value != null)
                    {
                        TimeDistribution td = new TimeDistribution
                        {
                            Time = int.Parse(row.Cells[0].Value.ToString()),
                            Probability = decimal.Parse(row.Cells[1].Value.ToString())
                        };
                        system.InterarrivalDistribution.Add(td);
                    }
                }

                // قراءة عدد السيرفرات والعملاء
                int numServers = int.Parse(textBox2.Text);
                system.NumberOfServers = numServers;
                system.StoppingNumber = int.Parse(textBox1.Text);
                system.StoppingCriteria = Enums.StoppingCriteria.NumberOfCustomers;

                // قراءة كل سيرفر
                for (int i = 0; i < numServers; i++)
                {
                    DataGridView dgv = panel1.Controls.Find($"serverTable{i + 1}", true).FirstOrDefault() as DataGridView;
                    if (dgv != null)
                    {
                        Server server = new Server();
                        server.ID = i + 1;
                        foreach (DataGridViewRow r in dgv.Rows)
                        {
                            if (r.Cells[0].Value != null && r.Cells[1].Value != null)
                            {
                                server.TimeDistribution.Add(new TimeDistribution
                                {
                                    Time = int.Parse(r.Cells[0].Value.ToString()),
                                    Probability = decimal.Parse(r.Cells[1].Value.ToString())
                                });
                            }
                        }
                        system.Servers.Add(server);
                    }
                }

                // تشغيل المحاكاة
                system.RunSimulation();

                // عرض النتائج في الجدول
                dataGridViewResults.Rows.Clear();
                foreach (var c in system.SimulationTable)
                {
                    dataGridViewResults.Rows.Add(
                        c.CustomerNumber,
                        c.RandomInterArrival,
                        c.InterArrival,
                        c.ArrivalTime,
                        c.RandomService,
                        c.ServiceTime,
                        c.AssignedServer.ID,
                        c.StartTime,
                        c.EndTime,
                        c.TimeInQueue
                    );
                }

                // عرض الملخص
                string summary = $"Average Waiting Time: {system.PerformanceMeasures.AverageWaitingTime}\n" +
                                 $"Waiting Probability: {system.PerformanceMeasures.WaitingProbability}\n" +
                                 $"Max Queue Length: {system.PerformanceMeasures.MaxQueueLength}";
                MessageBox.Show(summary, "Simulation Results");
                string testingResult = TestingManager.Test(system, Constants.FileNames.TestCase1);
                MessageBox.Show(testingResult);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }
    }
}
