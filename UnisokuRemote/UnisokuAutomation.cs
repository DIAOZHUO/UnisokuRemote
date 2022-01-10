using System;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;


namespace UnisokuRemote
{



    class UnisokuAutomation
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int MessageBox(IntPtr hWnd, String text, String caption, uint type);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern int EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpfn, int lParam);
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        [DllImport("user32.dll", EntryPoint = "FindWindowEx", SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, uint hwndChildAfter, string lpszClass, string lpszWindow);


        public const string mainFormTitle = "SPC-STG Remote (ID:0) Ver 2.0.0.0";

        private static UnisokuAutomation _instance = null;
        public static UnisokuAutomation Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new UnisokuAutomation();
                }
                return _instance;
            }
        }

        public IntPtr hWnd;
        AutomationElement mainForm;
        AutomationElement axis3Panel;
        InvokePattern zeroButton;
        RangeValuePattern zSlider;

        public double ZSliderValue
        {
            get { return zSlider.Current.Value; }
        }

        public bool Init()
        {

            hWnd = FindWindow(null, mainFormTitle);
            if (hWnd != null && hWnd != IntPtr.Zero)
            {
                mainForm = AutomationElement.FromHandle(hWnd);
                var axis3HWnd = FindWindowEx(hWnd, "Axis 3");
                axis3Panel = AutomationElement.FromHandle(axis3HWnd);

                //zeroButton = FindElementById(axis3Panel, "266410").GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
                //zSlider = FindElementById(axis3Panel, "266298").GetCurrentPattern(RangeValuePattern.Pattern) as RangeValuePattern;

                Console.WriteLine(axis3Panel.Current.Name + axis3Panel.Current.AutomationId);

                //zeroButton = AutomationElement.FromHandle(IntPtr.Add(axis3HWnd, 10)).GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
                //zSliderElement = AutomationElement.FromHandle(IntPtr.Add(axis3HWnd, 12));

                double _zeroYBottom = double.MaxValue;
                double _zSliderBottom = double.MaxValue;
                var list = FindInRawView(axis3Panel);
                foreach(var it in list)
                {
                    if (it.Current.Name == "Zero" && it.Current.ClassName == "TButton")
                    {
                        TreeWalker tree = TreeWalker.ControlViewWalker;
                        if (tree.GetParent(it).Current.Name == "Axis 3")
                        {
                            if (it.Current.BoundingRectangle.Bottom < _zeroYBottom)
                            {
                                _zeroYBottom = it.Current.BoundingRectangle.Bottom;
                                zeroButton = it.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;

                                Console.WriteLine("!Detect Button ID:" + it.Current.AutomationId + " " + it.Current.BoundingRectangle);
                            }
                        }

                        
                    }
                    if (it.Current.ClassName == "TUTrackBarEx")
                    {
                        TreeWalker tree = TreeWalker.ControlViewWalker;
                        if (tree.GetParent(it).Current.Name == "Axis 3")
                        {
                            if (it.Current.BoundingRectangle.Bottom < _zSliderBottom)
                            {
                                _zSliderBottom = it.Current.BoundingRectangle.Bottom;
                                zSlider = it.GetCurrentPattern(RangeValuePattern.Pattern) as RangeValuePattern;

                                Console.WriteLine("!Detect TrackBar ID:" + it.Current.AutomationId + " " + it.Current.BoundingRectangle);
                            }
                        }
                    }
                    //Console.WriteLine(it.Current.Name + " " + it.Current.ClassName + " " + it.Current.AutomationId);
                }

                if (zeroButton == null || zSlider == null)
                {
                    return false;
                }
                //zSlider = zSliderElement.GetCurrentPattern(RangeValuePattern.Pattern) as RangeValuePattern;

                return true;



            }

            return false;


        }

        public void MoveZLeft()
        {
            var v = Utility.Clamp<double>(zSlider.Current.Value - zSlider.Current.SmallChange, zSlider.Current.Minimum, zSlider.Current.Maximum);
            zSlider.SetValue(v);

        }

        public void MoveZRight()
        {
            var v = Utility.Clamp<double>(zSlider.Current.Value + zSlider.Current.SmallChange, zSlider.Current.Minimum, zSlider.Current.Maximum);
            zSlider.SetValue(v);
        }

        public void MoveZZero()
        {
            zeroButton.Invoke();
        }



        private static IEnumerable<AutomationElement> FindElementsByName(AutomationElement rootElement, string name)
        {
            var cnd = new PropertyCondition(AutomationElement.NameProperty, name);
            return rootElement.FindAll(TreeScope.Element | TreeScope.Descendants, cnd).Cast<AutomationElement>();
        }
        private static InvokePattern FindInvokePatternByName(AutomationElement rootElement, string name)
        {
            return FindElementsByName(rootElement, name).FirstOrDefault().GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
        }
        private static InvokePattern FindButtonInvokePatternByName(AutomationElement rootElement, string name)
        {
            return FindElementsByName(rootElement, name).Where(x => x.Current.ClassName == "Button").FirstOrDefault().GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
        }

        private static AutomationElement FindElementById(AutomationElement rootElement, string automationId)
        {
            return rootElement.FindFirst(
                TreeScope.Element | TreeScope.Descendants,
                new PropertyCondition(AutomationElement.AutomationIdProperty, automationId));
        }

        private static IEnumerable<AutomationElement> FindInRawView(AutomationElement root)
        {
            TreeWalker rawViewWalker = TreeWalker.RawViewWalker;
            Queue<AutomationElement> queue = new Queue<AutomationElement>();
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                var element = queue.Dequeue();
                yield return element;

                var sibling = rawViewWalker.GetNextSibling(element);
                if (sibling != null)
                {
                    queue.Enqueue(sibling);
                }

                var child = rawViewWalker.GetFirstChild(element);
                if (child != null)
                {
                    queue.Enqueue(child);
                }
            }
        }



        private IntPtr FindWindowEx(IntPtr hwnd, string windowTitle, bool bChild = true)
        {
            IntPtr iResult = IntPtr.Zero;
            // 首先在父窗体上查找控件  
            iResult = FindWindowEx(hwnd, 0, null, windowTitle);
            // 如果找到直接返回控件句柄  
            if (iResult != IntPtr.Zero) return iResult;

            // 如果设定了不在子窗体中查找  
            if (!bChild) return iResult;

            // 枚举子窗体，查找控件句柄  
            int i = EnumChildWindows(hwnd, (h, l) =>
             {
                 IntPtr f1 = FindWindowEx(h, 0, null, windowTitle);
                 if (f1 == IntPtr.Zero)
                     return true;
                 else
                 {
                     iResult = f1;
                     return false;
                 }
             }, 0);

            return iResult;
        }




    }

    public static class Utility
    {
        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }
    }
}
