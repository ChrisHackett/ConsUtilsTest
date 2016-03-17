using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace consutil1 {

    delegate void AccumInfo(string val);

    class Interpret1 {
        static void Main(string[] args) {
            Operator1 oper = new Operator1(myAccumInfo);
            int resVal = oper.Main(args);
            Console.WriteLine("\n Interpret1.log begin (result={0}) ",resVal);
            foreach (var var in errInfo) {
                Console.WriteLine(var);
            }
            Console.WriteLine("\n Interpret1.log end");
        }

        static void myAccumInfo(string val) {
            lock(errInfo) {
                errInfo.Add(val);
            }
        }

        static List<string>  errInfo = new List<string>();
    }

    class Operator1 {
        private AccumInfo myLog = null;
        private int lastErr = 0;
        public Operator1(AccumInfo errorLog) {
            myLog = errorLog ?? myAccumInfo;
        }

        List<string> errInfo = new List<string>();
        void myAccumInfo(string val) {
            lock (errInfo) {
                errInfo.Add(val);
            }
        }

        public int Main(string[] args) {
            int rVal = 0;
            StringDictionary opts;
            StringDictionary pars;
            int argsErr = getOpts(args, out opts, out pars);
            Console.WriteLine("Operator1.Main.opts returns: {0:X}({0})", argsErr);

            foreach (String nam in opts.Keys) {
                try {
                    switch (nam) {
                        case "help":
                            Console.WriteLine("no help here");
                            break;
                    }
                }
                catch (Exception exc) {
                    rVal = exc.HResult;
                    string msg = errMsg(exc, "Operator1.Main.opts");
                    Console.WriteLine(msg);
                    myLog(msg);
                }
            }


            DateTime dtStt = DateTime.Now;
            foreach (String nam in pars.Keys) {
                try {
                    switch (nam) {
                        case "oper":
                            switch (pars[nam]) {
                                case "copy": {
                                        rVal = OperCopy(opts, pars);
                                    }
                                    break;

                            }
                            break;
                    }
                }
                catch (Exception exc) {
                    rVal = exc.HResult;
                    string msg = errMsg(exc, "Operator1.Main.pars");
                    Console.WriteLine(msg);
                    myLog(msg);
                }
            }
            DateTime dtStp = DateTime.Now;
            Console.WriteLine("msecs="+dtStp.Subtract(dtStt).TotalMilliseconds);
            return rVal;
        }

        string errMsg(Exception exc, string where = "[not specified]") {
            return String.Format("Exception in '{3}': {0,8:X}({0}) {1}:{2}", exc.HResult, exc.Message, (exc.InnerException != null) ? exc.InnerException.Message : "[no inner]", where);
        }

        int getOpts(IEnumerable<string> args, out StringDictionary opts, out StringDictionary pars) {
            int rVal = 0;
            opts = new StringDictionary();
            pars = new StringDictionary();
            foreach (String stgVal in args) {
                string nam = stgVal;
                try {
                    bool isOpt = false;
                    if (nam[0].Equals('-')) {
                        isOpt = true;
                        nam = nam.Substring(1);
                    }
                    int eqIdx = 0;
                    string val = "";
                    if ((nam.Length >= 1) && (eqIdx = nam.IndexOf("=")) > 0) {
                        val = nam.Substring(eqIdx + 1);
                        nam = nam.Substring(0, eqIdx);
                    }
                    StringDictionary targ = (isOpt) ? opts : pars;
                    if (targ[nam] != null) {
                        targ[nam] = val;
                    }
                    else {
                        targ.Add(nam, val);
                    }
                    if (isOpt && nam.Equals("ThRoW")) {
                        int iv = int.Parse(val);
                        throw new Exception(val);
                    }
                }
                catch (Exception exc) {
                    if (rVal == 0) rVal = exc.HResult;
                    string msg = errMsg(exc, "Operator1.Main.opts");
                    //Console.WriteLine(msg);
                    myLog(msg);
                }
            }

            if (opts["v"] != null) {
                foreach (String stg in opts.Keys) {
                    Console.WriteLine("o {0,-10} = '{1}'", stg, opts[stg]);
                }
                foreach (String stg in pars.Keys) {
                    Console.WriteLine("p {0,-10} = '{1}'", stg, pars[stg]);
                }

            }

            return rVal;
        }

        int OperCopy(StringDictionary opts, StringDictionary pars) {
            string src = pars["src"];
            string des = pars["des"];
            if (src == null) DoLogError("need [src] directory ");
            if (des == null) DoLogError("need [des] directory ");
            if (src.Equals(des)) DoLogError("src can not equal des ");
            if (lastErr != 0) return lastErr;

            if (!Directory.Exists(src)) DoLogError("[src] directory does not exist");
            if (Directory.Exists(des)) DoLogError("[des] directory exists already",0);
            if (lastErr != 0) return lastErr;

            string forceCopyRoot = @"g:\junk\_des";
            string tv = opts["fd"];
            if ((tv = opts["fd"]) != null) {
                forceCopyRoot = tv;
            }
            if (forceCopyRoot.Length > 0) {
                if (!(des.IndexOf(forceCopyRoot) == 0)) {
                    DoLogError("currently forcing [des] root to :" + forceCopyRoot, 2);
                }
            }

            if (lastErr != 0) return lastErr;

            DirectoryInfo disrc = new DirectoryInfo(src);
            List<string> filesList = new List<string>();
            WalkDirectoryTree(disrc, filesList);
            Console.WriteLine("found:" + filesList.Count);
            return lastErr;
        }


        void WalkDirectoryTree(System.IO.DirectoryInfo root, List<string> filesList ) {
            System.IO.FileInfo[] files = null;
            System.IO.DirectoryInfo[] subDirs = null;

            // First, process all the files directly under this folder
            try {
                files = root.GetFiles("*.*");
            }
            // This is thrown if even one of the files requires permissions greater
            // than the application provides.
            catch (UnauthorizedAccessException e) {
                // This code just writes out the message and continues to recurse.
                // You may decide to do something different here. For example, you
                // can try to elevate your privileges and access the file again.
                DoLogError(errMsg(e, "WalkDirectoryTree"));
            }

            catch (System.IO.DirectoryNotFoundException e) {
                Console.WriteLine(e.Message);
                DoLogError(errMsg(e, "WalkDirectoryTree"));
            }

            catch (Exception e) {
                Console.WriteLine(e.Message);
                DoLogError(errMsg(e, "WalkDirectoryTree"));
            }


            if (files != null) {
                foreach (System.IO.FileInfo fi in files) {
                    // In this example, we only access the existing FileInfo object. If we
                    // want to open, delete or modify the file, then
                    // a try-catch block is required here to handle the case
                    // where the file has been deleted since the call to TraverseTree().
                    Console.WriteLine(fi.FullName);
                    filesList.Add(fi.FullName);
                }

                // Now find all the subdirectories under this directory.
                subDirs = root.GetDirectories();

                foreach (System.IO.DirectoryInfo dirInfo in subDirs) {
                    // Resursive call for each subdirectory.
                    WalkDirectoryTree(dirInfo, filesList);
                }
            }
        }

        int DoLogError(string errMsg, int errVal = -1) {
            if (errVal != 0) lastErr = errVal;
            myLog(errVal + " " + errMsg);
            return lastErr;
        }


    }
}
