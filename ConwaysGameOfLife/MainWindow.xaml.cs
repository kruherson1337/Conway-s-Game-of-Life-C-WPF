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
        // Background thread for game
        private readonly BackgroundWorker worker = new BackgroundWorker();

        // Game properties
        private const int MAX_NUMBER_OF_GENERERATION_WITHOUT_CHANGE = 10;
        private bool[][] grid;
        private long generation;
        private long liveCells;
        private long oldLiveCells;
        private long generationNothingChanged;

        // Status properties
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

        // Canvas properties
        private int resolution;
        private int width;
        private int height;

        public MainWindow()
        {
            InitializeComponent();

            // Prepare background thread
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerCompleted += worker_Completed;
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;

            // Set preferences
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
            // Calculate width and height based on resolution
            width = (int)kanvas.ActualWidth / resolution;
            height = (int)kanvas.ActualHeight / resolution;

            // Initialize grid
            grid = create2DArray();

            // Randomize grid
            random();

            // Draw grid
            draw();

        }

        private void compute()
        {
            // Compute next generation
            bool[][] next = create2DArray();

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    // Get state and neighbors
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
                    // Skip center
                    if (i == 0 && j == 0)
                        continue;

                    // get neighbor position
                    int col = (x + i + width) % width;
                    int row = (y + j + height) % height;

                    // Sum all live neighbors
                    count += grid[col][row] ? 1 : 0;
                }
            }

            return count;
        }

        private void draw()
        {
            // Clear up canvas
            kanvas.Children.Clear();

            // Count live cells
            int countLiveCells = 0;

            // Draw grid
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    // Get position
                    int x = i * resolution;
                    int y = j * resolution;

                    // Rectangle color (Live-Black, Dead-White)
                    Brush color;
                    if (grid[i][j])
                    {
                        // Live cell
                        color = Brushes.Black;
                        countLiveCells++;
                    }
                    else
                    {
                        // Death cell
                        color = Brushes.White;
                    }

                    // Create rectangle
                    Rectangle rect = new Rectangle()
                    {
                        Width = resolution,
                        Height = resolution,
                        Fill = color,
                        Stroke = Brushes.Black,
                        StrokeThickness = 1,
                        Name = String.Format("rectX{0}X{1}", i, j) // Save position
                    };

                    // Add click feature
                    rect.MouseDown += Rect_MouseDown;

                    // Add to canvas
                    kanvas.Children.Add(rect);
                    Canvas.SetLeft(rect, x);
                    Canvas.SetTop(rect, y);
                }
            }

            // Display live cells
            LiveCells = countLiveCells;
        }

        private bool stoppingCondition()
        {
            // Check if live cell count changed
            if (oldLiveCells == liveCells)
                generationNothingChanged++;
            else
                generationNothingChanged = 0;

            // Save current live cell count
            oldLiveCells = liveCells;

            // Check if we pass threshold
            if (generationNothingChanged >= MAX_NUMBER_OF_GENERERATION_WITHOUT_CHANGE)
                return true;
            else
                return false;
        }

        private void Rect_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Get clicked rectangle
            Rectangle rect = (Rectangle)sender;

            // Get position in grid
            String[] position = rect.Name.Split('X');
            int i = Int32.Parse(position[1]);
            int j = Int32.Parse(position[2]);

            // Flip value
            grid[i][j] = !grid[i][j];

            // Draw grid
            draw();
        }

        private void reset()
        {
            // Reset values
            Generation = 0;
            LiveCells = 0;
            oldLiveCells = 0;
            generationNothingChanged = 0;

            // Clear grid
            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                    grid[i][j] = false;
        }

        private bool[][] create2DArray()
        {
            // Initialize new 2D array
            bool[][] grid = new bool[width][];
            for (int i = 0; i < width; i++)
                grid[i] = new bool[height];
            return grid;
        }

        private void random()
        {
            // Randomize grid
            Random rand = new Random(42);
            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                    grid[i][j] = rand.NextDouble() >= 0.5;
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            // Check if stop running
            while (!stoppingCondition())
            {
                // Check if player stop game
                if (worker.CancellationPending)
                    break;

                // Compute new generation
                compute();

                // Draw new generation
                worker.ReportProgress((int)generation);

                // Delay - nicer animation
                Thread.Sleep(150);
            }

            MessageBox.Show("Game ended.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // After computing next generation
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
                // get rid of managed resources
                worker.Dispose();
            }
            // get rid of unmanaged resources
        }

        private void ButtonRun_Click(object sender, RoutedEventArgs e)
        {
            // Check if already running
            if (!worker.IsBusy)
            {
                // Run background worker
                buttonRun.Content = "Stop";
                worker.RunWorkerAsync();
            }
            else
            {
                // Stop background worker
                worker.CancelAsync();
            }
        }

        private void ButtonStep_Click(object sender, RoutedEventArgs e)
        {
            // Check if game ended
            if (stoppingCondition())
            {
                MessageBox.Show("Game ended.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Compute next generation
            compute();

            // Increment generation
            Generation++;

            // Draw grid 
            draw();
        }

        private void ButtonClear_Click(object sender, RoutedEventArgs e)
        {
            // Reset grid
            reset();

            // Draw grid
            draw();
        }

        private void ButtonRandom_Click(object sender, RoutedEventArgs e)
        {
            // Reset values
            Generation = 0;

            // Randomize grid
            random();

            // Draw grid
            draw();
        }

        private void textboxResolution_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                // Change resolution
                TextBox textBox = (TextBox)sender;
                resolution = Int32.Parse(textBox.Text);

                // Reload grid
                loadGrid();
            }
            catch (Exception ignored)
            {

            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Reload grid
            loadGrid();
        }
    }
}
