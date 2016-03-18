using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace consutil1 {

    delegate void AccumInfo(string val);

    delegate void Walker(System.IO.DirectoryInfo root, List<string> dirsList);

    class Interpret1 {
        static void Main(string[] args) {
            FileSystemOps oper = new FileSystemOps(myAccumInfo);
            int resVal = oper.Main(args);
            Console.WriteLine("\n Interpret1.log begin (result={0}) ", resVal);
            foreach (var var in errInfo) {
                Console.WriteLine(var);
            }
            Console.WriteLine("\n Interpret1.log end");
        }

        static void myAccumInfo(string val) {
            lock (errInfo) {
                errInfo.Add(val);
            }
        }

        static List<string> errInfo = new List<string>();
    }


    class SimpleUtils {
        public static int ExtractArguements(IEnumerable<string> args, out StringDictionary opts, out StringDictionary pars, AccumInfo myLog) {
            int rVal = 0;
            opts = new StringDictionary();
            pars = new StringDictionary();
            foreach (string stgVal in args) {
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
                    string msg = ExceptionMsg(exc, "FileSystemOps.Main.opts");
                    //Console.WriteLine(msg);
                    myLog(msg);
                }
            }

            if (opts["v"] != null) {
                foreach (string stg in opts.Keys) {
                    Console.WriteLine("o {0,-10} = '{1}'", stg, opts[stg]);
                }
                foreach (string stg in pars.Keys) {
                    Console.WriteLine("p {0,-10} = '{1}'", stg, pars[stg]);
                }

            }

            return rVal;
        }

        public static string ExceptionMsg(Exception exc, string where = "[not specified]") {
            return string.Format("Exception in '{3}': {0,8:X}({0}) {1}:{2}", exc.HResult, exc.Message, (exc.InnerException != null) ? exc.InnerException.Message : "[no inner]", where);
        }

        public static string ErrorMsg(string errorTxt, int errorVal, string where = "[not specified]") {
            return string.Format("Error '{3}': {0,8:X}({0}) {1}:{2}", errorVal, errorTxt, where, errorTxt);
        }
        public static string InfoMsg(string errorTxt, int errorVal, string where = "[not specified]") {
            return string.Format("Info '{3}': {0,8:X}({0}) {1}:{2}", errorVal, errorTxt, where, errorTxt);
        }



    }



    class FileSystemOps {
        private AccumInfo myLog = null;
        private int lastErr = 0;
        public FileSystemOps(AccumInfo errorLog) {
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
            int argsErr = SimpleUtils.ExtractArguements(args, out opts, out pars, myLog);
            Console.WriteLine("FileSystemOps.Main.opts returns: {0:X}({0})", argsErr);

            foreach (string nam in opts.Keys) {
                try {
                    switch (nam) {
                        case "help":
                            Console.WriteLine("no help here");
                            break;
                    }
                }
                catch (Exception exc) {
                    rVal = exc.HResult;
                    string msg = SimpleUtils.ExceptionMsg(exc, "FileSystemOps.Main.opts");
                    Console.WriteLine(msg);
                    myLog(msg);
                }
            }

            List<string> resultFinalList = new List<string>();

            DateTime dtStt = DateTime.Now;
            foreach (string nam in pars.Keys) {
                try {
                    switch (nam) {
                        case "oper":
                            switch (pars[nam]) {
                                case "copy": {
                                        //List<string> srcFilesList = new List<string>();
                                        rVal = TraverseAndCollect(opts, pars, resultFinalList, WalkDirectoryFileTree);
                                        if (rVal != 0) {
                                            string msg = SimpleUtils.ErrorMsg("aborting copy on error", rVal, "oper.copy walk");
                                            Console.WriteLine(msg);
                                            myLog(msg);
                                        }
                                        else {
                                            CopyFiles(opts, pars, resultFinalList);
                                        }
                                    }
                                    break;

                                case "findd": {
                                        List<string> srcDirsList = new List<string>();
                                        rVal = TraverseAndCollect(opts, pars, srcDirsList, WalkDirectoryTree);
                                        if (rVal != 0) {
                                            string msg = SimpleUtils.ErrorMsg("aborting copy on error", rVal, "oper.copy walk");
                                            Console.WriteLine(msg);
                                            myLog(msg);
                                        }
                                        else {
                                            List<string> desDirsList = new List<string>();
                                            rVal = DedupList(opts, pars, srcDirsList, desDirsList);  // dedupped dirs list
                                            Console.WriteLine("Directory List length:" + desDirsList.Count);
                                            string pattern = pars["pat"];
                                            if (pattern != null) {
                                                string[] pats = pattern.Split(',');
                                                foreach (string pat in pats) {
                                                    if (pat.Length > 0) {
                                                        foreach (string dir in desDirsList) {
                                                            if (dir.EndsWith(pat)) {
                                                                resultFinalList.Add(dir);
                                                            }
                                                        }
                                                    }
                                                }
                                                Console.WriteLine("Filtered List length:" + resultFinalList.Count);
                                            }
                                            else {
                                                resultFinalList = desDirsList;
                                            }


                                        }
                                    }
                                    break;
                            }
                            break;
                    }
                }
                catch (Exception exc) {
                    rVal = exc.HResult;
                    string msg = SimpleUtils.ExceptionMsg(exc, "FileSystemOps.Main.pars");
                    Console.WriteLine(msg);
                    myLog(msg);
                }
            }
            DateTime dtStp = DateTime.Now;
            Console.WriteLine("msecs=" + dtStp.Subtract(dtStt).TotalMilliseconds);

            if ((opts["v"] != null) && (opts["v"].Equals("1"))) {
                foreach (string stg in resultFinalList) {
                    Console.WriteLine(stg);
                }
                Console.WriteLine("Directory List end");
            }

            return rVal;
        }





        int TraverseAndCollect(StringDictionary opts, StringDictionary pars, List<string> srcFilesList, Walker walkMethod) {
            string src = pars["src"];
            if (src == null) DoLogError("need [src] directory ");
            if (lastErr != 0) return lastErr;

            if (!Directory.Exists(src)) DoLogError("[src] directory does not exist");
            if (lastErr != 0) return lastErr;

            DirectoryInfo disrc = new DirectoryInfo(src);
            walkMethod(disrc, srcFilesList);
            Console.WriteLine("found:" + srcFilesList.Count);
            return lastErr;
        }



        int CopyFiles(StringDictionary opts, StringDictionary pars, List<string> srcFilesList) {
            string des = pars["des"];
            if (des == null) DoLogError("need [des] directory ");
            if (lastErr != 0) return lastErr;

            if (Directory.Exists(des)) DoLogError("[des] directory exists already", 0);
            if (lastErr != 0) return lastErr;

            string forceCopyRoot = @"g:\junk\_des";
            string tv = opts["fd"];
            if ((tv = opts["fd"]) != null) {
                forceCopyRoot = tv;
            }
            if (forceCopyRoot.Length > 0) {
                if (!(des.IndexOf(forceCopyRoot) == 0)) {
                    DoLogError(" requires [des] root as :" + forceCopyRoot, 2);
                }
            }
            if (lastErr != 0) return lastErr;

            List<string> desFilesList = new List<string>();  // Accumulate src/des pairs so can parallel copies
            Hashtable directoryNames = new Hashtable();  // to cut down on probes for create
            char sdSepChar = '|';
            foreach (var sName in srcFilesList) {
                FileInfo sfi = new FileInfo(sName);
                string srcPath = sfi.DirectoryName;
                //string sFrag = srcPath.Substring(src.Length);
                string dPath = Path.Combine(des, srcPath.Substring(3));
                if (directoryNames[dPath] == null) {
                    if (!Directory.Exists(dPath)) {
                        Directory.CreateDirectory(dPath);
                        directoryNames[dPath] = 1;
                    }
                }
                string dName = Path.Combine(dPath, sfi.Name);
                desFilesList.Add(sName + sdSepChar + dName);
            }
            ParallelLoopResult result = Parallel.ForEach(desFilesList, n => {
                string[] sdPair = n.Split(sdSepChar);
                File.Copy(sdPair[0], sdPair[1]);
            });
            myLog(SimpleUtils.InfoMsg("parallel copy result", (result.IsCompleted)?0:1, "oper.copy walk"));
            return lastErr;
        }


        private void CombinePaths(string p1, string p2) {

            try {
                string combination = Path.Combine(p1, p2);

                Console.WriteLine("When you combine '{0}' and '{1}', the result is: '{2}'",
                            p1, p2, combination);
            }
            catch (Exception e) {
                if (p1 == null)
                    p1 = "null";
                if (p2 == null)
                    p2 = "null";
                Console.WriteLine("You cannot combine '{0}' and '{1}' because: {2}{3}",
                            p1, p2, Environment.NewLine, e.Message);
            }

            Console.WriteLine();
        }


        int DedupList(StringDictionary opts, StringDictionary pars, List<string> srcFilesList, List<string> desDirsList) {
            Hashtable htDirs = new Hashtable();
            foreach (var sName in srcFilesList) {
                if (htDirs[sName] == null) {
                    htDirs.Add(sName, 1);
                    desDirsList.Add(sName);
                }
            }
            myLog(SimpleUtils.InfoMsg("found", desDirsList.Count, "DedupList"));
            return lastErr;
        }

        void WalkDirectoryTree(System.IO.DirectoryInfo root, List<string> dirsList) {
            //System.IO.DirectoryInfo[] subDirs = null;

            //try {
            //    subDirs = root.GetDirectories();
            //}
            //// This is thrown if even one of the files requires permissions greater than the application provides.
            //catch (UnauthorizedAccessException e) {
            //    // This code just writes out the message and continues to recurse.
            //    // You may decide to do something different here. For example, you
            //    // can try to elevate your privileges and access the file again.
            //    DoLogError(SimpleUtils.ExceptionMsg(e, "WalkDirectoryFileTree UnauthorizedAccessException"));
            //}
            //catch (System.IO.DirectoryNotFoundException e) {
            //    Console.WriteLine(e.Message);
            //    DoLogError(SimpleUtils.ExceptionMsg(e, "WalkDirectoryFileTree DirectoryNotFoundException"));
            //}
            //catch (Exception e) {
            //    Console.WriteLine(e.Message);
            //    DoLogError(SimpleUtils.ExceptionMsg(e, "WalkDirectoryFileTree Exception"));
            //}

            dirsList.Add(root.FullName);
            //if (subDirs.Length != 0) {
                foreach (System.IO.DirectoryInfo dirInfo in root.GetDirectories()) {
                    // Resursive call for each subdirectory.
                    //dirsList.Add(dirInfo.FullName);
                    WalkDirectoryTree(dirInfo, dirsList);
                }
            //}
        }
        void WalkDirectoryFileTree(System.IO.DirectoryInfo root, List<string> filesList) {
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
                DoLogError(SimpleUtils.ExceptionMsg(e, "WalkDirectoryFileTree UnauthorizedAccessException"));
            }

            catch (System.IO.DirectoryNotFoundException e) {
                Console.WriteLine(e.Message);
                DoLogError(SimpleUtils.ExceptionMsg(e, "WalkDirectoryFileTree DirectoryNotFoundException"));
            }

            catch (Exception e) {
                Console.WriteLine(e.Message);
                DoLogError(SimpleUtils.ExceptionMsg(e, "WalkDirectoryFileTree Exception"));
            }


            if (files != null) {
                foreach (System.IO.FileInfo fi in files) {
                    // In this example, we only access the existing FileInfo object. If we
                    // want to open, delete or modify the file, then
                    // a try-catch block is required here to handle the case
                    // where the file has been deleted since the call to TraverseTree().
                    //Console.WriteLine(fi.FullName);
                    filesList.Add(fi.FullName);
                }

                // Now find all the subdirectories under this directory.
                subDirs = root.GetDirectories();

                foreach (System.IO.DirectoryInfo dirInfo in subDirs) {
                    // Resursive call for each subdirectory.
                    WalkDirectoryFileTree(dirInfo, filesList);
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
