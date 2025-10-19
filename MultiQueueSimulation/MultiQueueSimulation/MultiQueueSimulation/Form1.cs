using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MultiQueueModels;
using MultiQueueTesting;

namespace MultiQueueSimulation
{
    public partial class Form1 : Form
    {
        private List<DataGridView> serverTables = new List<DataGridView>();
        private int currentServerIndex = 0;
        private Button buttonNext;
        private Button buttonPrev;
        private Panel graphPanel;
        private Button nextGraph;
        private Button previousGraph;
        private int currentGraphServerIndex = 0;
        private SimulationSystem simulationSystem; // لتخزين النظام بعد التشغيل
        public Form1()
        {
            InitializeComponent();
            graphPanel = new Panel();
            graphPanel.Width = 1200;       // نفس عرض panel1
            graphPanel.Height = 270;                // ارتفاع مناسب
            graphPanel.Top = 350;   // أسفل panel1
            graphPanel.Left = 20;
            graphPanel.BorderStyle = BorderStyle.FixedSingle;
            graphPanel.BackColor = Color.White;    // يظهر دائمًا
            graphPanel.Paint += GraphPanel_Paint;  // Paint event للرسم المستمر
            this.Controls.Add(graphPanel);

            // زر السابق للرسم
            previousGraph = new Button();
            previousGraph.Text = "←";
            previousGraph.Width = 50;
            previousGraph.Top = graphPanel.Bottom + 5;
            previousGraph.Left = graphPanel.Left;
            previousGraph.Click += PreviousGraph_Click;
            this.Controls.Add(previousGraph);

            // زر التالي للرسم
            nextGraph = new Button();
            nextGraph.Text = "→";
            nextGraph.Width = 50;
            nextGraph.Top = graphPanel.Bottom + 5;
            nextGraph.Left = graphPanel.Left + 60;
            nextGraph.Click += NextGraph_Click;
            this.Controls.Add(nextGraph);
            comboBox1.Items.Add("Priority");
            comboBox1.Items.Add("Random");
            comboBox1.Items.Add("Least utilization");
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox1.SelectedIndex = 0;

            comboBox2.Items.Add("Maximum Number of customers");
            comboBox2.Items.Add("Simulation end time");
            comboBox2.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox2.SelectedIndex = 0;

            buttonPrev = new Button();
            buttonPrev.Text = "←";
            buttonPrev.Width = 50;
            buttonPrev.Click += buttonPrev_Click;

            buttonNext = new Button();
            buttonNext.Text = "→";
            buttonNext.Width = 50;
            buttonNext.Click += buttonNext_Click;

            panel1.Controls.Add(buttonPrev);
            panel1.Controls.Add(buttonNext);
        }
        private void GraphPanel_Paint(object sender, PaintEventArgs e)
        {
            if (simulationSystem == null) return;
            DrawGraphForServer(currentGraphServerIndex, e.Graphics);
        }
        private void DrawGraphForServer(int serverIndex, Graphics g)
        {
            if (simulationSystem == null || simulationSystem.SimulationTable.Count == 0) return;
            if (serverIndex < 0 || serverIndex >= simulationSystem.NumberOfServers) return;

            g.Clear(Color.White);

            Pen axisPen = new Pen(Color.Black, 1);
            Brush workBrush = new SolidBrush(Color.AliceBlue); // اللون البني
            Brush textBrush = Brushes.Black;

            int margin = 50;
            int panelWidth = graphPanel.Width - 2 * margin;
            int panelHeight = graphPanel.Height - 2 * margin;

            // رسم المحاور فقط
            g.DrawLine(axisPen, margin, panelHeight + margin, panelWidth + margin, panelHeight + margin); // X-axis
            g.DrawLine(axisPen, margin, margin, margin, panelHeight + margin); // Y-axis

            // Y-axis 0 → 1 بتسميات فقط
            for (double yVal = 0; yVal <= 1.0; yVal += 0.2)
            {
                int y = margin + panelHeight - (int)(yVal * panelHeight);
                g.DrawString(yVal.ToString("0.0"), new Font("Segoe UI", 8), textBrush, 5, y - 7);
            }

            // X-axis من 0 → نهاية آخر حدث فعلي بتسميات فقط
            double endTime = simulationSystem.SimulationTable.Max(c => c.EndTime);
            for (double t = 0; t <= endTime; t += 2)
            {
                int x = margin + (int)((t / endTime) * panelWidth);
                g.DrawString(t.ToString("0"), new Font("Segoe UI", 8), textBrush, x - 5, panelHeight + margin + 2);
            }

            // رسم البارات لكل فترة شغل السيرفر
            var serverEvents = simulationSystem.SimulationTable
                .Where(c => c.AssignedServer.ID == serverIndex + 1)
                .Select(c => new { Start = c.StartTime, End = c.EndTime })
                .ToList();

            foreach (var ev in serverEvents)
            {
                int x = margin + (int)((ev.Start / endTime) * panelWidth);
                int width = (int)(((ev.End - ev.Start) / endTime) * panelWidth);
                int y = margin;
                int barHeight = panelHeight;
                g.FillRectangle(workBrush, x, y, width, barHeight);
            }

            // تسمية المحاور
            g.DrawString("Time", new Font("Segoe UI", 10, FontStyle.Bold), textBrush, margin + panelWidth / 2 - 20, panelHeight + margin + 20);
            g.DrawString("B(t)", new Font("Segoe UI", 10, FontStyle.Bold), textBrush, 5, margin - 20);

            // عنوان السيرفر
            g.DrawString($"Server {serverIndex + 1}", new Font("Segoe UI", 12, FontStyle.Bold), textBrush, margin, 5);
        }




        private void NextGraph_Click(object sender, EventArgs e)
        {
            if (currentGraphServerIndex < simulationSystem.NumberOfServers - 1)
            {
                currentGraphServerIndex++;
                graphPanel.Invalidate(); // يعيد الرسم تلقائيًا
            }
        }

        private void PreviousGraph_Click(object sender, EventArgs e)
        {
            if (currentGraphServerIndex > 0)
            {
                currentGraphServerIndex--;
                graphPanel.Invalidate(); // يعيد الرسم تلقائيًا
            }
        }




        private void label1_Click(object sender, EventArgs e)
        {
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            dataGridView1.Columns.Add("InterarrivalTime", "Interarrival Time");
            dataGridView1.Columns.Add("Probability", "Probability");
            InitializeMainTable(5);
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;

            panel1.AutoScroll = true;

            dataGridViewResults.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewResults.AllowUserToAddRows = false;
            dataGridViewResults.AllowUserToDeleteRows = false;

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

            serverTables.Clear();
            panel1.Controls.Clear();

            if (int.TryParse(textBox1.Text, out numTables) && numTables > 0)
            {
                for (int i = 0; i < numTables; i++)
                {
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

                    serverTables.Add(dgv);
                }

                currentServerIndex = 0;
                ShowCurrentServerTable();
            }
        }

        private void ShowCurrentServerTable()
        {
            panel1.Controls.Clear();

            if (serverTables.Count == 0) return;

            Label lbl = new Label();
            lbl.Text = $"Server {currentServerIndex + 1} / {serverTables.Count}";
            lbl.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lbl.AutoSize = true;
            lbl.Left = 10;
            lbl.Top = 10;

            DataGridView currentTable = serverTables[currentServerIndex];
            currentTable.Top = lbl.Bottom + 5;
            currentTable.Left = 10;

            panel1.Controls.Add(lbl);
            panel1.Controls.Add(currentTable);

            // ضبط حجم الـ panel حسب الجدول
            int panelWidth = currentTable.Width + 40;
            int panelHeight = currentTable.Height + lbl.Height + 80;
            panel1.Width = panelWidth;
            panel1.Height = panelHeight;

            // وضع الأزرار داخل الـ panel
            buttonPrev.Top = currentTable.Bottom + 10;
            buttonPrev.Left = panel1.Width / 2 - 70;
            buttonNext.Top = currentTable.Bottom + 10;
            buttonNext.Left = panel1.Width / 2 + 20;

            panel1.Controls.Add(buttonPrev);
            panel1.Controls.Add(buttonNext);
        }

        private void buttonNext_Click(object sender, EventArgs e)
        {
            if (currentServerIndex < serverTables.Count - 1)
            {
                currentServerIndex++;
                ShowCurrentServerTable();
            }
        }

        private void buttonPrev_Click(object sender, EventArgs e)
        {
            if (currentServerIndex > 0)
            {
                currentServerIndex--;
                ShowCurrentServerTable();
            }
        }

        private void btnRunSimulation_Click(object sender, EventArgs e)
        {
            try
            {
                SimulationSystem system = new SimulationSystem();

                string selectedMethod = comboBox1.SelectedItem.ToString();
                if (selectedMethod == "Random")
                    system.SelectionMethod = Enums.SelectionMethod.Random;
                else if (selectedMethod == "Priority")
                    system.SelectionMethod = Enums.SelectionMethod.HighestPriority;
                else
                    system.SelectionMethod = Enums.SelectionMethod.LeastUtilization;

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

                int numServers = int.Parse(textBox1.Text);
                system.NumberOfServers = numServers;
                system.StoppingNumber = int.Parse(textBox2.Text);
                system.StoppingCriteria = Enums.StoppingCriteria.NumberOfCustomers;

                for (int i = 0; i < numServers; i++)
                {
                    if (i < serverTables.Count)
                    {
                        DataGridView dgv = serverTables[i];
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

                system.RunSimulation();

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
                simulationSystem = system; // تخزين النظام للرسم
                currentGraphServerIndex = 0;
                graphPanel.Invalidate();   // يظهر الرسم لأول سيرفر

                string summary = $"Average Waiting Time: {system.PerformanceMeasures.AverageWaitingTime}\n" +
                                 $"Waiting Probability: {system.PerformanceMeasures.WaitingProbability}\n" +
                                 $"Max Queue Length: {system.PerformanceMeasures.MaxQueueLength}";
                MessageBox.Show(summary, "Simulation Results");

                string testingResult = TestingManager.Test(system, Constants.FileNames.TestCase2);
                MessageBox.Show(testingResult);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }
    }
}
