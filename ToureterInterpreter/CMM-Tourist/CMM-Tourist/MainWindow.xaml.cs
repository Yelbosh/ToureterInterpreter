using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Timers;
using System.Windows.Threading;
using AvalonDock.Layout;
using System.Diagnostics;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using AvalonDock.Layout.Serialization;
using System.Resources;
using System.Collections;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using System.Windows.Forms;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Folding;
using JDYCompiler;
using System.Threading;


namespace CMM_Tourist
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private JDYCompiler.IInterpreter iIterpreter;
        private Thread mBackgroundThread = null;

        System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
        System.Windows.Forms.StatusStrip statusStrip = new StatusStrip();
        System.Windows.Forms.SaveFileDialog saveFileDialog = new SaveFileDialog();
        System.Windows.Forms.MenuStrip menuStrip = new MenuStrip();

        public MainWindow()
        {
            iIterpreter = new JDYCompiler.JDYCompiler();
            InitializeComponent();
            this.DataContext = this;
            
            //console.SetValue( "123213";

        }





        private void dockManager_DocumentClosing(object sender, AvalonDock.DocumentClosingEventArgs e)
        {
            if (System.Windows.MessageBox.Show("Are you sure you want to close the document?", "AvalonDock Sample", MessageBoxButton.YesNo) == MessageBoxResult.No)

                e.Cancel = true;

        }

        private void InputTbx_SelectionChanged(object sender, EventArgs e)
        {

            var input = sender as TextEditor;
            int row, col = 1;
            string text = input.Text.Substring(0, input.SelectionStart);
            string[] tokens = text.Split(new string[] { "\n" }, StringSplitOptions.None);
            row = tokens.Length;
            if (tokens.Length - 1 >= 0)
                col = tokens[tokens.Length - 1].Length + 1;
            ready.Content = "Row " + row + "      Col " + col;
            try1.Content = textEditor.SyntaxHighlighting;





        }


        /*****************以下是FILE菜单项的方法*/

        private void NewFile(object sender, RoutedEventArgs e)
        {
            var firstDocumentPane = docManager.Layout.Descendents().OfType<LayoutDocumentPane>().FirstOrDefault();
            try
            {
                if (firstDocumentPane != null)
                {
                    LayoutDocument doc = new LayoutDocument();
                    string title = "New File 1";
                    string contentId = "NewFile1";
                    int i = 2;
                    while (docManager.Layout.Descendents().OfType<LayoutDocument>().Any(d => d.Title == title))
                    {
                        title = "New File " + i;
                        contentId = "NewFile" + i;
                        i++;
                    }
                    TextEditor ted = new TextEditor();
                    ted.Style = textEditor.Style;
                    ted.Template = textEditor.Template;
                    ted.ShowLineNumbers = true;
                    ted.SyntaxHighlighting = textEditor.SyntaxHighlighting;
                    ted.TextChanged += InputTbx_SelectionChanged;
                    doc.Title = title;
                    doc.ContentId = contentId;
                    doc.Content = ted;

                    firstDocumentPane.Children.Add(doc);
                }
            }
            catch (Exception error_e)
            {
                error.Text = error_e.Message;
            }



            //var leftAnchorGroup = docManager.Layout.LeftSide.Children.FirstOrDefault();
            //if (leftAnchorGroup == null)
            //{
            //    leftAnchorGroup = new LayoutAnchorGroup();
            //    docManager.Layout.LeftSide.Children.Add(leftAnchorGroup);
            //}

            //leftAnchorGroup.Children.Add(new LayoutAnchorable() { Title = "New Anchorable" });
        }

        private void treeview_recently(string str_name, string str_soure)
        {

            System.Windows.Controls.TreeViewItem treeview_item = new TreeViewItem();

            treeview_item.Header = str_name;

            treeview_item.DataContext = str_soure;

            treeview_item.Foreground = treeview_FilesResource.Foreground;






        }

        

        private void OnSaveBtn(object sender, RoutedEventArgs e)
        {
            saveFileDialog.FileName = "";
            saveFileDialog.Filter = "CMM source files (*.cmm)|*.cmm|All files (*.*)|*.*";

            Stream myStream = null;

            if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    if ((myStream = saveFileDialog.OpenFile()) != null)
                    {
                        // 保存代码至指定文件
                        StreamWriter sr = new StreamWriter(myStream, System.Text.Encoding.GetEncoding("GB2312"));
                        sr.Write(textEditor.Text);
                        sr.Close();
                        myStream.Close();
                        statusStrip.Text = "File saved";
                    }
                }
                catch (Exception error_e)
                {
                    statusStrip.Text = "File saving failed";
                    error.Text = error_e.Message + " File saving failed";
                }
            }
        }

        private void Add_click(object sender, RoutedEventArgs e)
        {
            var FirstDocumentPane = docManager.Layout.Descendents().OfType<LayoutDocumentPane>().FirstOrDefault();
            LayoutDocument new_document = new LayoutDocument();
            TextEditor text_new = new TextEditor();
            text_new.Style = textEditor.Style;
            text_new.Template = textEditor.Template;
            text_new.ShowLineNumbers = true;
            text_new.SyntaxHighlighting = textEditor.SyntaxHighlighting;
            text_new.TextChanged += InputTbx_SelectionChanged;

            openFileDialog.FileName = "";
            openFileDialog.Filter = "CMM source files (*.cmm)|*.cmm|All files (*.*)|*.*";

            Stream myStream = null;

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    if ((myStream = openFileDialog.OpenFile()) != null)
                    {
                        // 读取文件内容至代码框
                        StreamReader sr = new StreamReader(myStream, System.Text.Encoding.GetEncoding("GB2312"));
                        text_new.Text = sr.ReadToEnd();
                        sr.Close();
                        myStream.Close();
                        statusStrip.Text = "File opened";
                        new_document.Title = openFileDialog.SafeFileName;
                        new_document.Content = text_new;
                        FirstDocumentPane.Children.Add(new_document);
                        new_document.IsActive = true;
                    }
                }
                catch (Exception error_e)
                {
                    statusStrip.Text = "File opening failed";
                    System.Windows.MessageBox.Show(error_e.Message + " File opening failed.", "Error");
                }
            }
        }



        /*****************FILE菜单项的方法结束*/

        /*****************以下是EDIT菜单项的方法*/
        private void OnUndoBtn(object sender, RoutedEventArgs e)
        {
            textEditor.Undo();
        }

        private void OnCopyBtn(object sender, RoutedEventArgs e)
        {
            textEditor.Copy();
        }

        private void OnPasteBtn(object sender, RoutedEventArgs e)
        {
            textEditor.Paste();

        }

        private void OnCutBtn(object sender, RoutedEventArgs e)
        {
            textEditor.Cut();
        }

        private void OnRedoBtn(object sender, RoutedEventArgs e)
        {
            textEditor.Redo();
        }
        /*****************EDIT菜单项的方法结束*/




        /*****************以下是view菜单项的方法*/
        private void lexicalAnalysis_click(object sender, RoutedEventArgs e)
        {
            var LexicalAnalysisWindow1 = docManager.Layout.Descendents().OfType<LayoutAnchorable>().SingleOrDefault(a => a.ContentId == "LexicalAnalysis");
            if (LexicalAnalysisWindow1.IsHidden)
                LexicalAnalysisWindow1.Show();
            else if (LexicalAnalysisWindow1.IsVisible)
                LexicalAnalysisWindow1.IsActive = true;
            else
            {
                LexicalAnalysisWindow1.AddToLayout(docManager, AnchorableShowStrategy.Bottom | AnchorableShowStrategy.Most);
            }
        }

        private void SyntaxAnalysis_click(object sender, RoutedEventArgs e)
        {
            var SyntaxAnalysisWindow1 = docManager.Layout.Descendents().OfType<LayoutAnchorable>().SingleOrDefault(a => a.ContentId == "SyntaxAnalysis");
            if (SyntaxAnalysisWindow1.IsHidden)
                SyntaxAnalysisWindow1.Show();
            else if (SyntaxAnalysisWindow1.IsVisible)
                SyntaxAnalysisWindow1.IsActive = true;
            else
            {
                SyntaxAnalysisWindow1.AddToLayout(docManager, AnchorableShowStrategy.Bottom | AnchorableShowStrategy.Most);
            }
        }

        private void SemanticAnalysis_click(object sender, RoutedEventArgs e)
        {
            var SemanticAnalysisWindow1 = docManager.Layout.Descendents().OfType<LayoutAnchorable>().SingleOrDefault(a => a.ContentId == "SemanticAnalysis");
            if (SemanticAnalysisWindow1.IsHidden)
                SemanticAnalysisWindow1.Show();
            else if (SemanticAnalysisWindow1.IsVisible)
                SemanticAnalysisWindow1.IsActive = true;
            else
            {
                SemanticAnalysisWindow1.AddToLayout(docManager, AnchorableShowStrategy.Bottom | AnchorableShowStrategy.Most);
            }
        }

        private void IntermediateCode_click(object sender, RoutedEventArgs e)
        {
            var IntermediateCodeWindow1 = docManager.Layout.Descendents().OfType<LayoutAnchorable>().SingleOrDefault(a => a.ContentId == "IntermediateCode");
            if (IntermediateCodeWindow1.IsHidden)
                IntermediateCodeWindow1.Show();
            else if (IntermediateCodeWindow1.IsVisible)
                IntermediateCodeWindow1.IsActive = true;
            else
            {
                IntermediateCodeWindow1.AddToLayout(docManager, AnchorableShowStrategy.Bottom | AnchorableShowStrategy.Most);
            }
        }

        private void Console_click(object sender, RoutedEventArgs e)
        {
            var ConsoleWindow1 = docManager.Layout.Descendents().OfType<LayoutAnchorable>().SingleOrDefault(a => a.ContentId == "Console");
            if (ConsoleWindow1.IsHidden)
                ConsoleWindow1.Show();
            else if (ConsoleWindow1.IsVisible)
                ConsoleWindow1.IsActive = true;
            else
            {
                ConsoleWindow1.AddToLayout(docManager, AnchorableShowStrategy.Bottom | AnchorableShowStrategy.Most);
            }
        }

        private void ErrorList(object sender, RoutedEventArgs e)
        {
            var errorWindow1 = docManager.Layout.Descendents().OfType<LayoutAnchorable>().SingleOrDefault(a => a.ContentId == "error1");
            if (errorWindow1 == null)
            {
                errorWindow1.AddToLayout(docManager, AnchorableShowStrategy.Bottom | AnchorableShowStrategy.Most);
                return;
            }

            if (errorWindow1.IsHidden)
                errorWindow1.Show();
            else if (errorWindow1.IsVisible)
                errorWindow1.IsActive = true;
        }

        private void Maximum(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                this.WindowState = WindowState.Maximized;
                Max.Header = "Normal";
                return;
            }

            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                Max.Header = "Maximized";
                return;
            }
        }

        private void Minimum(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Code_click(object sender, RoutedEventArgs e)
        {
            if (Code_view.IsVisible)
                Code_view.Children.LastOrDefault().IsActive = true;
        }

        private void FilesResource_click(object sender, RoutedEventArgs e)
        {
            var FilesResource = docManager.Layout.Descendents().OfType<LayoutAnchorable>().SingleOrDefault(a => a.ContentId == "FilesResource");
            if (FilesResource.IsHidden)
                FilesResource.Show();
            else if (FilesResource.IsVisible)
                FilesResource.IsActive = true;
            else
            {
                FilesResource.AddToLayout(docManager, AnchorableShowStrategy.Left | AnchorableShowStrategy.Most);
            }
        }

        /*****************是view菜单项的方法结束*/





        /*****************以下是RUN菜单项的方法*/


        private void Analysis(object sender ,RoutedEventArgs e)
        {
            var AnalysisDocumentPane = docManager.Layout.Descendents().OfType<LayoutDocumentPane>().SingleOrDefault(a => a.SelectedContent.IsLastFocusedDocument);

            TextEditor text_select = AnalysisDocumentPane.SelectedContent.Content as TextEditor;

            try
            {
                text_select.SelectAll();

                console.DataContext = text_select.Text;

                if (text_select.Text != "" && text_select.Text != null)
                {
                    iIterpreter.InputSource(text_select.Text);

                    LexicalAnalysis.Text = iIterpreter.LexicalAnalyze();
                    SyntaxAnalysis.Text = iIterpreter.GrammerAnalyze();
                    IntermediateCode.Text = iIterpreter.GetIntermediaCode();
                    SemanticAnalysis.Text = iIterpreter.SemanticAnalyze();
                    try1.Content = "Analysis done successfully.";
                }
                else
                {
                    System.Windows.MessageBox.Show("Please input your code!", "Empty code");
                }
            }

            catch (ParseException exception)
            {
                error.Text = exception.Message;
                System.Windows.MessageBox.Show("Oppa, ParseException occured! Check your code plz", "Parse Error :(");
                try1.Content = "Error occurred";
                throw exception;
            }
            catch (Exception exception) {
                error.Text = exception.Message;
                System.Windows.MessageBox.Show("Opps, Some UNKNOWN Exception occured, you need debug!", "Erroc :(");
                throw exception;
            }
        }

 
        /*****************RUN菜单项的方法结束*/




        /*****************以下是TEST菜单项的方法*/

        private void Variable_test(object sender, RoutedEventArgs e)
        {

        }

        private void Array_test(object sender, RoutedEventArgs e)
        {

        }

        private void If_statement(object sender, RoutedEventArgs e)
        {

        }

        private void While_statement(object sender, RoutedEventArgs e)
        {

        }

        private void Nested_statement(object sender, RoutedEventArgs e)
        {

        }

        private void Symbol_table(object sender, RoutedEventArgs e)
        {

        }

        private void Function_test(object sender, RoutedEventArgs e)
        {

        }

        /*****************TEST菜单项的方法结束*/




        /*****************以下是ABOUT菜单项的方法*/

        private void Version_information(object sender, RoutedEventArgs e)
        {

        }

        /*****************ABOUT菜单项的方法结束*/



        private void OnToolWindow1Hiding(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (System.Windows.MessageBox.Show("Are you sure you want to hide this tool?", "AvalonDock", MessageBoxButton.YesNo) == MessageBoxResult.No)
                e.Cancel = true;
        }

        private void OnLayoutRootPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //var activeContent = ((LayoutRoot)sender).ActiveContent;
            //if (e.PropertyName == "ActiveContent")
            //{
            //    Debug.WriteLine(string.Format("ActiveContent-> {0}", activeContent));
            //}
        }


        //语言选择高亮
        AbstractFoldingStrategy _foldingStrategy;
        FoldingManager _foldingManager;
        void HighlightingComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // SyntaxHighlighting - это свойство, определяющее текущее правило подсветки синтаксиса
            if (textEditor.SyntaxHighlighting == null)
            {
                _foldingStrategy = null;
            }
            else
            {
                switch (textEditor.SyntaxHighlighting.Name)
                {
                    case "XML":
                        _foldingStrategy = new XmlFoldingStrategy();
                        textEditor.TextArea.IndentationStrategy = new ICSharpCode.AvalonEdit.Indentation.DefaultIndentationStrategy();
                        break;
                    case "C#":
                    case "C++":
                    case "PHP":
                    case "Java":
                        textEditor.TextArea.IndentationStrategy = new ICSharpCode.AvalonEdit.Indentation.CSharp.CSharpIndentationStrategy(textEditor.Options);
                        _foldingStrategy = null;
                        break;
                    case "CMM":
                        textEditor.TextArea.IndentationStrategy = new ICSharpCode.AvalonEdit.Indentation.CSharp.CSharpIndentationStrategy();
                        break;
                    default:
                        textEditor.TextArea.IndentationStrategy = new ICSharpCode.AvalonEdit.Indentation.DefaultIndentationStrategy();
                        _foldingStrategy = null;
                        break;
                }
            }
            if (_foldingStrategy != null)
            {
                if (_foldingManager == null)
                    _foldingManager = FoldingManager.Install(textEditor.TextArea);
                _foldingStrategy.UpdateFoldings(_foldingManager, textEditor.Document);
            }
            else
            {
                if (_foldingManager != null)
                {
                    FoldingManager.Uninstall(_foldingManager);
                    _foldingManager = null;
                }
            }
        }



        private void OnExit(object sender, RoutedEventArgs e)
        {
            this.Close();
        }




        private void OnRunBtn(object sender, RoutedEventArgs e)
        {
            StartBackgorundRunThread();
        }

        private void Windows_Load(object sender, RoutedEventArgs e)
        {
            //加载最近打开
            using (FileStream fs = new FileStream("1.txt", FileMode.Open))
            {
                using (StreamReader r = new StreamReader(fs))
                {

                    var tmp = new List<string>();

                    //foreach (string i in tmp)
                    //{
                    //    tmp.Add(r.ReadLine());
                    //    if (string.IsNullOrEmpty(i))
                    //    {
                    //        int n = tmp.Count;
                    //        tmp.Remove(n - 1);
                    //        break;
                    //    }
                    //}
                    //if (tmp.Count > 10)
                    //{
                    //    for (int m = 0; m < 10; m++)
                    //    {
                    //        tmp[m] = tmp[m + 1];
                    //    }
                    //}



                    //lists.Add("1");

                    string data = r.ReadLine();
                    while (data != null)
                    {
                        tmp.Add(data);
                        data = r.ReadLine();
                    }

                    tmp.Reverse();

                    foreach (string temp in tmp)
                    {

                        System.Windows.Controls.MenuItem history = new System.Windows.Controls.MenuItem();//定义子菜单
                        History.Items.Add(history);
                        history.Header = temp;
                        history.Tag = temp;
                        history.Width = 500;
                        history.FontSize = 12;

                        history.Style = NewMenu.Style;

                        history.Click += history_Click;
                    }
                }
            }
        }

        private void history_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.MenuItem history = sender as System.Windows.Controls.MenuItem;
            history.Header = history.Tag.ToString();


            //加载到界面
            var firstDocumentPane = docManager.Layout.Descendents().OfType<LayoutDocumentPane>().FirstOrDefault();

            if (firstDocumentPane != null)
            {
                LayoutDocument doc = new LayoutDocument();
                string title = history.Header.ToString().Split(',')[1];
                string contentId = "NewFile1";
                int i = 2;
                while (docManager.Layout.Descendents().OfType<LayoutDocument>().Any(d => d.Title == title))
                {
                    title = history.Header.ToString();
                    contentId = "NewFile" + i;
                    i++;
                }
                TextEditor ted = new TextEditor();
                ted.Style = textEditor.Style;
                ted.Template = textEditor.Template;
                ted.ShowLineNumbers = true;
                ted.SyntaxHighlighting = textEditor.SyntaxHighlighting;
                ted.Load(history.Tag.ToString().Split(',')[0]);
                doc.Title = history.Header.ToString().Split(',')[1];
                doc.ContentId = contentId;
                doc.Content = ted;
                firstDocumentPane.Children.Add(doc);
                doc.IsActive = true;
            }

        }

        private void OnDeleteMenu(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(textEditor.SelectedText)) ;
            else textEditor.SelectedText = textEditor.SelectedText.Remove(0);
        }

        private void StartBackgorundRunThread()
        {
            if (this.mBackgroundThread!=null && this.mBackgroundThread.ThreadState == System.Threading.ThreadState.Running)
            {
                mBackgroundThread.Abort();
            }
            mBackgroundThread = new Thread(this.Run);
            mBackgroundThread.Start();
        }


        private void TreeViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem tvt = sender as TreeViewItem;
            try
            {
                switch ( tvt.DataContext.ToString() )
                {
                    case "1":
                        addToTree("../../testcode/变量赋值与赋值测试.CMM");
                        break;
                    case "2":
                        addToTree("../../testcode/数组声明与赋值测试.CMM");
                        break;
                    case "3":
                        addToTree("../../testcode/If 语句测试.CMM");
                        break;
                    case "4":
                        addToTree("../../testcode/while语句测试.CMM");
                        break;
                    case "5":
                        addToTree("../../testcode/嵌套语句测试.CMM");
                        break;
                    case "6":
                        addToTree("../../testcode/符号表测试.CMM");
                        break;
                    case "7":
                        addToTree("../../testcode/功能测试举例.CMM");
                        break;
                    default:
                        break;
                }
            }

            catch (Exception ee)
            {
                System.Windows.MessageBox.Show(ee.Message, "error");
 
            }
        }

        private void addToTree(string load)
        {
            var FirstDocumentPane = docManager.Layout.Descendents().OfType<LayoutDocumentPane>().FirstOrDefault();
            LayoutDocument new_document = new LayoutDocument();
            TextEditor text_new = new TextEditor();
            text_new.Style = textEditor.Style;
            text_new.Template = textEditor.Template;
            text_new.ShowLineNumbers = true;
            text_new.SyntaxHighlighting = textEditor.SyntaxHighlighting;
            text_new.TextChanged += InputTbx_SelectionChanged;

                      
            //Stream myStream = null;
                try
                {
                        text_new.Load(load);
                        new_document.Title = System.IO.Path.GetFileName(load);
                        new_document.Content = text_new;
                        FirstDocumentPane.Children.Add(new_document);
                        new_document.IsActive = true;
                }
                catch (Exception error_e)
                {
                    statusStrip.Text = "File opening failed";
                    System.Windows.MessageBox.Show(error_e.Message + " File opening failed.", "Error");
                }
        }

        private void Run()
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                try
                {
                    iIterpreter = new JDYCompiler.JDYCompiler();
                    Analysis(null, null);
                    console.Document = new FlowDocument(new Paragraph(new Run(iIterpreter.Interprete())));
                }
                catch (Exception exception) { 
                    
                }
            })); 
            iIterpreter.AsynchronStart();
            
           
            //while (true)
            //{
            //    ToureterRunState state = iIterpreter.GetToureterRunningState();
            //    if (state == ToureterRunState.WAIT_INPUT)
            //    {
            //                    this.Dispatcher.Invoke((Action)(() =>
            //{
            //            console.AppendText(iIterpreter.GetConsoleOutput());
            //            console.IsReadOnly=false;
            //})); 
            //    }
            //    else if (state == ToureterRunState.ENDED)
            //    {
            //                                        this.Dispatcher.Invoke((Action)(() =>
            //{
            //            console.AppendText(iIterpreter.GetConsoleOutput());
            //            console.IsReadOnly = true;
            //}));
            //                                        break;
            //    }
            //}
        }

        private void Console_OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key== Key.RightShift)
            {
                iIterpreter.ConsoleInput("Nimeia");
                //mBackgroundThread.Resume();
            }
        }
    }
}



