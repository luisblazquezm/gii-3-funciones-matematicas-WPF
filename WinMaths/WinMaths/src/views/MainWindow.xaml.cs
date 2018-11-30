﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WinMaths.src.views;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WinMaths.src.bean;
using WinMaths.src.viewModels;
using WinMaths.src.utils;
using System.IO;
using WinMaths.src.bean.function;

namespace WinMaths
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private PreferencesMenuUI PreferencesMenuUIVar;
        private FunctionRepresentationUtils FunctionRepresentationVar;
        private ViewModel viewModel;

        private FuncRect real;
        private FuncRect screen;

        // A partir de una determinada grafica obtengo la representación de su polilinea
        private Dictionary<Graphic, Polyline> graphicRepresentationDictionary;
        private ScaleTransform scaleTransform;
        private Point _last;

        private double scaleRate;
        private bool added, isDragged;

        public double Xmin { get; private set; }
        public double Xmax { get; private set; }
        public double Ymin { get; private set; }
        public double Ymax { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            /* Variables globales */
            scaleTransform = new ScaleTransform();
            graphicRepresentationDictionary = null;
            scaleRate = 1.1;
            
            /* Evento de cierre de las ventanas */
            this.Closed += Window_Closed;

            // Instanciación de la clase FunctionRepresentationUtils
            FunctionRepresentationVar = new FunctionRepresentationUtils();

            // Gestión de la instancia del ViewModel
            viewModel = new ViewModel();
            viewModel.GraphicSetToDraw += ViewModel_GraphicSetToDraw;
            viewModel.GraphicRepresentationUpdated += ViewModel_GraphicRepresentationUpdated;

            // Gestión del Grid que contiene al canvas
            GridOfCanvas.MouseMove += GridOfCanvas_MouseMove;
            GridOfCanvas.MouseLeftButtonUp += GridOfCanvas_MouseLeftButtonUp;
            GridOfCanvas.MouseLeftButtonDown += GridOfCanvas_MouseLeftButtonDown;

            // Gestión del Canvas de Representación
            this.SizeChanged += RepresentationCanvas_SizeChanged;
            RepresentationCanvas.MouseWheel += RepresentationCanvas_MouseWheel;
            RepresentationCanvas.MouseMove += RepresentationCanvas_MouseMove;

            // Gestión de exportar el canvas a imagen
            ExportMenuOption.Click += ExportButton_Click;

            SettingsButton.Click += SettingsButton_Click;

            // Gestión del Borde que contiene el canvas
            clipBorder.MouseEnter += ClipBorder_MouseEnter;
            clipBorder.MouseLeave += ClipBorder_MouseLeave;

            // Gestión de la instancia de la ventana del menú de preferencias
            PreferencesMenuUIVar = new PreferencesMenuUI(viewModel);
            PreferencesMenuUIVar.Closed += Window_Closed;
            PreferencesMenuUIVar.Show();
        }

        private void ViewModel_GraphicSetToDraw(object sender, ViewModelEventArgs e)
        {
            List<Graphic> listOfGraphicsToRepresent = null;
            PointCollection[] graphicRepresentation = null;
            FuncRect funcR = viewModel.FuncRect;

            DrawAxisAndLines();

            listOfGraphicsToRepresent = (List<Graphic>)e.ListOfGraphics;

            funcR.XMin = -10;
            funcR.XMax = 10;
            funcR.YMin = -10;
            funcR.YMax = 10;

            if (graphicRepresentationDictionary == null) // Para que no se dibuje nada en el sizeChanged
                graphicRepresentationDictionary = new Dictionary<Graphic, Polyline>();

            foreach (Graphic g in listOfGraphicsToRepresent)
            {
                if (graphicRepresentationDictionary.ContainsKey(g) && g != null) {
                    Polyline line = graphicRepresentationDictionary[g];
                    RepresentationCanvas.Children.Add(line);
                } else {
                        

                    graphicRepresentation = FunctionRepresentationVar.DrawGraphic(g, RepresentationCanvas.ActualWidth, RepresentationCanvas.ActualHeight, funcR);
                    foreach (PointCollection p in graphicRepresentation) {
                        Polyline line = new Polyline()
                        {
                            Points = p,
                            Stroke = new SolidColorBrush(g.GraphicColor)
                        };

                        /*
                        if (g.Function.Formula.Equals(ExponentialFunction.GetFormula()) && g.ParamB < 0 ||
                            g.Function.Formula.Equals(FractionalFunction.GetFormula()))
                        {
                            Console.WriteLine("Oh shit");
                                
                        }
                                */
                        graphicRepresentationDictionary.Add(g, line);
                        Console.WriteLine("-o-");
                        RepresentationCanvas.Children.Add(line);
                    }
                }
            }
        }

        private void ViewModel_GraphicRepresentationUpdated(object sender, ViewModelEventArgs e)
        {
            PointCollection[] graphicRepresentation = null;
            FuncRect funcR = viewModel.FuncRect;
        
            RepresentationCanvas.Children.Clear();
            DrawAxisAndLines();
            funcR = viewModel.FuncRect;

            foreach (Graphic g in graphicRepresentationDictionary.Keys)
            {
                Console.WriteLine("AJA --> ENTRE Xmin {0} Xmax{1} Y {2}{3}", funcR.XMin, funcR.XMax, funcR.YMin, funcR.YMax);
                graphicRepresentation = FunctionRepresentationVar.DrawGraphic(g, RepresentationCanvas.ActualWidth, RepresentationCanvas.ActualHeight, funcR);
                foreach (PointCollection p in graphicRepresentation)
                {
                    Polyline line = new Polyline()
                    {
                        Points = p,
                        Stroke = new SolidColorBrush(g.GraphicColor)
                    };

                    /*
                    if (graphicRepresentationDictionary.ContainsKey(g) && g != null) {
                        graphicRepresentationDictionary.Remove(g);
                        graphicRepresentationDictionary.Add(g, line);
                    }
                    */
                    RepresentationCanvas.Children.Add(line);
                }
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }
        
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void RepresentationCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RepresentationCanvas.Children.Clear();
            PointCollection[] graphicRepresentation = null;
            FuncRect funcR = viewModel.FuncRect;

            DrawAxisAndLines();

            if (graphicRepresentationDictionary != null) {
                List<Graphic> listOfGraphicsToRepresent = viewModel.GetListOfGraphicsVM();

                funcR.XMin = -10;
                funcR.XMax = 10;
                funcR.YMin = -10;
                funcR.YMax = 10;

                if (listOfGraphicsToRepresent != null)
                {
                    foreach (Graphic g in listOfGraphicsToRepresent)
                    {
                        graphicRepresentation = FunctionRepresentationVar.DrawGraphic(g, RepresentationCanvas.ActualWidth, RepresentationCanvas.ActualHeight, funcR);
                        
                        foreach (PointCollection p in graphicRepresentation)
                        {
                            graphicRepresentationDictionary.Remove(g);
                            Polyline line = new Polyline()
                            {
                                Points = p,
                                Stroke = new SolidColorBrush(g.GraphicColor)
                            };
                            //graphicRepresentationDictionary.Add(g, line);
                            RepresentationCanvas.Children.Add(line);
                        }
                        
                    }
                }
            }

        }

        private void DrawAxisAndLines()
        {
            // <<>>>>>><<<<<<<<< aqui iran los limites de X e Y 
            real = FunctionRepresentationVar.DeclareFuncRect(-10, 10, -10, 10); // Real
            screen = FunctionRepresentationVar.DeclareFuncRect(0, RepresentationCanvas.ActualWidth, 0, RepresentationCanvas.ActualHeight); // Screen
            Boolean ejeHorizontal = true;
            Boolean ejeVertical = false;
            double distancia = 0.333;
            double limitX = RepresentationCanvas.ActualWidth;
            double limitY = RepresentationCanvas.ActualHeight;

            Console.WriteLine(limitX);

            // Eje X e Y
            foreach (Line l in FunctionRepresentationVar.DrawAxis(real, screen))
                RepresentationCanvas.Children.Add(l);

            // Lineas Eje X
            foreach (Line l in FunctionRepresentationVar.DrawAxisLines(screen, real, real.XMin, real.YMax, distancia, ejeHorizontal))
                RepresentationCanvas.Children.Add(l);

            // Lineas Eje Y
            foreach (Line l in FunctionRepresentationVar.DrawAxisLines(screen, real, real.XMin, real.YMax, distancia, ejeVertical))
                RepresentationCanvas.Children.Add(l);
        }

        private void RepresentationCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0){
                scaleTransform.ScaleX *= scaleRate;
                scaleTransform.ScaleY *= scaleRate;
            } else {
                scaleTransform.ScaleX /= scaleRate;
                scaleTransform.ScaleY /= scaleRate;
            }

            if (!added) {
                TransformGroup tg = (TransformGroup) RepresentationCanvas.RenderTransform;
                if (tg != null) {
                    tg.Children.Add(scaleTransform);
                    RepresentationCanvas.RenderTransformOrigin = new Point(0.5, 0.5);
                    added = true;
                }
            }
        }

        private void RepresentationCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            Panel MousePanel = (Panel)sender;
            Point p = e.GetPosition(RepresentationCanvas);

            Console.WriteLine("Position Mouse Canvas X:{0} Y:{1}", p.X, p.Y);

            XCoordLabel.Content = FunctionRepresentationVar.ConvertXFromPantToReal(p.X, RepresentationCanvas.ActualWidth, screen, real);
            YCoordLabel.Content = FunctionRepresentationVar.ConvertYFromPantToReal(p.Y, RepresentationCanvas.ActualHeight, screen, real);
        }

        private void ClipBorder_MouseLeave(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Arrow;
        }

        private void ClipBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Cross;
        }

        private void GridOfCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragged == false)
                return;

            base.OnMouseMove(e);
            if (e.LeftButton == MouseButtonState.Pressed && GridOfCanvas.IsMouseCaptured)
            {
                Point pos = e.GetPosition(GridOfCanvas);
                Matrix matrix = mt.Matrix;
                matrix.Translate(pos.X - _last.X, pos.Y - _last.Y);
                mt.Matrix = matrix;
                _last = pos;
            }

        }

        private void GridOfCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            GridOfCanvas.ReleaseMouseCapture();
            isDragged = false;
        }

        private void GridOfCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            GridOfCanvas.CaptureMouse();
            _last = e.GetPosition(GridOfCanvas);
            isDragged = true;
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = "NuevaImagen",
                Filter = "JPeg Image | *.jpg | Bitmap Image | *.bmp | PNG | *.png | Gif Image | *.gif"
            };

            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                SaveToPng(RepresentationCanvas, dlg.FileName);
            }
        }

        public void SaveToPng(FrameworkElement visual, string fileName)
        {
            var encoder = new PngBitmapEncoder();
            SaveUsingEncoder(visual, fileName, encoder);
        }

        private void SaveUsingEncoder(FrameworkElement visual, string fileName, BitmapEncoder encoder)
        {
            RenderTargetBitmap bitmap = new RenderTargetBitmap(
                (int)visual.ActualWidth,
                (int)visual.ActualHeight,
                96,
                96,
                PixelFormats.Pbgra32);
            bitmap.Render(visual);
            BitmapFrame frame = BitmapFrame.Create(bitmap);
            encoder.Frames.Add(frame);

            using (var stream = File.Create(fileName))
            {
                encoder.Save(stream);
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            FuncRect funcRect = viewModel.FuncRect;

            RepresentationLimitsWindow representatitonLimits = new RepresentationLimitsWindow()
            {
                Xmin = funcRect.XMin,
                Xmax = funcRect.XMax,
                Ymin = funcRect.YMin,
                Ymax = funcRect.YMax
            };

            representatitonLimits.ShowDialog();
            if (false == representatitonLimits.LimitsChanged)
                return;

            funcRect.XMin = representatitonLimits.Xmin;
            funcRect.XMax = representatitonLimits.Xmax;
            funcRect.YMin = representatitonLimits.Ymin;
            funcRect.YMax = representatitonLimits.Ymax;

            viewModel.FuncRect = funcRect;
        }
    }
}
