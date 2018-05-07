using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ConwaysGameOfLife
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDisposable
    {
        private readonly BackgroundWorker worker = new BackgroundWorker();
        
        private bool[][] grid;
        private long generation;
        private long liveCells;
        private long oldLiveCells;
        private long generationNothingChanged;

        public long Generation
        {
            get { return generation; }
            set
            {
                generation = value;
                labelGeneration.Content = String.Format("Generation: {0}", generation);
            }
        }
        public long LiveCells
        {
            get { return liveCells; }
            set
            {
                liveCells = value;
                labeliveCells.Content = String.Format("Live Cells: {0}", liveCells);
            }
        }

        private int resolution;
        private int width;
        private int height;

        public MainWindow()
        {
            InitializeComponent();

            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerCompleted += worker_Completed;
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;

            Generation = 0;
            resolution = 20;
            LiveCells = 0;
            oldLiveCells = 0;
            generationNothingChanged = 0;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            loadGrid();
        }

        private void loadGrid()
        {
            width = (int)kanvas.ActualWidth / resolution;
            height = (int)kanvas.ActualHeight / resolution;

            grid = create2DArray();

            random();

            draw();
        }

        private void compute()
        {
            bool[][] next = create2DArray();

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    bool state = grid[i][j];
                    int neighbors = countNeighbors(grid, i, j);

                    if (state == false && neighbors == 3)
                    {
                        // Any dead cell with exactly three live neighbours becomes a live cell, as if by reproduction.
                        next[i][j] = true;
                    }
                    else if (state == true && (neighbors < 2 || neighbors > 3))
                    {
                        // Any live cell with fewer than two live neighbours dies, as if caused by underpopulation.
                        // Any live cell with more than three live neighbours dies, as if by overpopulation.
                        next[i][j] = false;
                    }
                    else if (state == true && (neighbors == 2 || neighbors == 3))
                    {
                        // Any live cell with two or three live neighbours lives on to the next generation.
                        next[i][j] = true;
                    }
                    else
                    {
                        // Nothing
                        next[i][j] = false;
                    }
                }
            }

            grid = next;
        }

        private int countNeighbors(bool[][] grid, int x, int y)
        {
            int count = 0;

            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    if (i == 0 && j == 0)
                        continue;

                    int col = (x + i + width) % width;
                    int row = (y + j + height) % height;

                    count += grid[col][row] ? 1 : 0;
                }
            }

            return count;
        }

        private void draw()
        {
            kanvas.Children.Clear();

            int countLiveCells = 0;

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    int x = i * resolution;
                    int y = j * resolution;

                    Brush color;
                    if (grid[i][j])
                    {
                        color = Brushes.Black;
                        countLiveCells++;
                    }
                    else
                    {
                        color = Brushes.White;
                    }

                    Rectangle rect = new Rectangle()
                    {
                        Width = resolution,
                        Height = resolution,
                        Fill = color,
                        Stroke = Brushes.Black,
                        StrokeThickness = 1,
                        Name = String.Format("rectX{0}X{1}", i, j)
                    };

                    rect.MouseDown += Rect_MouseDown;

                    kanvas.Children.Add(rect);
                    Canvas.SetLeft(rect, x);
                    Canvas.SetTop(rect, y);
                }
            }

            LiveCells = countLiveCells;
        }

        private bool stoppingCondition()
        {
            if (oldLiveCells == liveCells)
                generationNothingChanged++;
            else
                generationNothingChanged = 0;

            oldLiveCells = liveCells;

            if (generationNothingChanged >= width)
                return true;
            else
                return false;
        }

        private void Rect_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Rectangle rect = (Rectangle)sender;

            String[] position = rect.Name.Split('X');
            int i = Int32.Parse(position[1]);
            int j = Int32.Parse(position[2]);

            grid[i][j] = !grid[i][j];

            draw();
        }

        private void reset()
        {
            Generation = 0;
            LiveCells = 0;
            oldLiveCells = 0;
            generationNothingChanged = 0;

            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                    grid[i][j] = false;
        }

        private bool[][] create2DArray()
        {
            bool[][] grid = new bool[width][];      
            for (int i = 0; i < width; i++)
                grid[i] = new bool[height];
            return grid;
        }

        private void random()
        {
            Random rand = new Random(42);
            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                    grid[i][j] = rand.NextDouble() >= 0.5;
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!stoppingCondition())
            {
                if (worker.CancellationPending)
                    break;

                compute();

                worker.ReportProgress((int)generation);

                Thread.Sleep(150);
            }

            MessageBox.Show("Game ended.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            draw();
            Generation++;
        }

        void worker_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            buttonRun.Content = "Start";
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                worker.Dispose();
            }
        }

        private void ButtonRun_Click(object sender, RoutedEventArgs e)
        {
            if (!worker.IsBusy)
            {
                buttonRun.Content = "Stop";
                worker.RunWorkerAsync();
            }
            else
            {
                worker.CancelAsync();
            }
        }

        private void ButtonStep_Click(object sender, RoutedEventArgs e)
        {
            if (stoppingCondition())
            {
                MessageBox.Show("Game ended.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            compute();

            Generation++;

            draw();
        }

        private void ButtonClear_Click(object sender, RoutedEventArgs e)
        {
            reset();

            draw();
        }

        private void ButtonRandom_Click(object sender, RoutedEventArgs e)
        {
            Generation = 0;

            random();

            draw();
        }

        private void textboxResolution_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                TextBox textBox = (TextBox)sender;
                resolution = Int32.Parse(textBox.Text);

                loadGrid();
            }
            catch (Exception ignored)
            {

            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            loadGrid();
        }
    }
}
