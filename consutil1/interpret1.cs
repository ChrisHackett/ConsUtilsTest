using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


// touch to push downstream; touching again; and again
// touching for iss1


// moving forward on iss1 after hotfix checkin

// and another touch back at the master branch
// and hotfix branch touch

// and one more final touch after all merged in

namespace consutil1 {

    delegate void AccumInfo(string val);

    delegate void Walker(DirectoryInfo root, List<string> dirsList);


    class Interpret1 {

    //const string hooksNoGit = @"\w*(?<!.git\)hooks";


        static void Main(string[] args) {
            FileSystemOps oper = new FileSystemOps(myAccumInfo);
            List<String> results = oper.FilterFSInfo(args,null,null);
            Console.WriteLine("\n Interpret1.log  (result List items={0}) ", results.Count);
            lock (errInfo) {
                foreach (var var in errInfo) {
                    Console.WriteLine(var);
                }
            }
            Console.WriteLine("\n Interpret1.log end");
        }
        /// <summary>
        /// accumulate some info, likely for output later.
        /// </summary>
        /// <param name="val"></param>
        static void myAccumInfo(string val) {
            lock (errInfo) {
                errInfo.Add(val);
            }
        }

        static readonly List<string> errInfo = new List<string>();
    }

    /// <summary>
    /// some simple statics for commandline processing, errror messages, etc.
    /// </summary>
    class SimpleUtils {
        public static int ExtractArguements(IEnumerable<string> args, out Dictionary<string,string> opts, out Dictionary<string,string> pars, AccumInfo myLog) {
            int rVal = 0;
            opts = new Dictionary<string,string>();
            pars = new Dictionary<string,string>();
            foreach (string stgVal in args) {
                string nam = stgVal;
                try {
                    bool isOpt = false;
                    if (nam[0].Equals('-')) {
                        isOpt = true;
                        nam = nam.Substring(1);
                    }
                    int eqIdx;
                    string val = "";
                    if ((nam.Length >= 1) && (eqIdx = nam.IndexOf("=")) > 0) {
                        val = nam.Substring(eqIdx + 1);
                        nam = nam.Substring(0, eqIdx);
                    }
                    Dictionary<string,string> targ = (isOpt) ? opts : pars;
                    if (targ.ContainsKey(nam)) {
                        targ[nam] = val;
                    }
                    else {
                        targ.Add(nam, val);
                    }
                    if (isOpt && nam.Equals("ThRoW")) {  // to test aborting
                        int iv = int.Parse(val);
                        if (iv != 0) {
                        }
                        throw new Exception(val);
                    }
                }
                catch (Exception exc) {
                    if (rVal == 0) rVal = exc.HResult;
                    string msg = ExceptionMsg(exc, "FileSystemOps.FilterFSInfo.opts");
                    //Console.WriteLine(msg);
                    myLog(msg);
                }
            }

            if (opts.ContainsKey("v")) {  // simple verbose
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


    /// <summary>
    /// Perform some operations on files and directories.
    /// THis is mainly to test alternative io patterns, test speed improvements on SSD, etc.
    /// Real world example for cloning git repos, for example, as well as duplicating lots of files quickly.
    /// On fast SSD with lots of medium to small files the parallel approach about doubles the throughput of what basic windows xcopy can do.
    /// </summary>
    class FileSystemOps {
        private readonly AccumInfo myLog;
        private int lastErr;
        public FileSystemOps(AccumInfo errorLog) {
            myLog = errorLog ?? myAccumInfo;
        }

        private readonly List<string> errInfo = new List<string>();
        void myAccumInfo(string val) {
            lock (errInfo) {
                errInfo.Add(val);
            }
        }

        public List<string> FilterFSInfo(string[] args, Dictionary<string,string> opts, Dictionary<string,string> pars) {
            if ((opts == null) || (pars == null)) {
                int argsErr = SimpleUtils.ExtractArguements(args, out opts, out pars, myLog);
                Console.WriteLine("FileSystemOps.FilterFSInfo.opts returns: {0:X}({0})", argsErr);
            }

            foreach (string nam in opts.Keys) {
                try {
                    switch (nam) {
                        case "help":
                            Console.WriteLine("no help here");
                            break;
                    }
                }
                catch (Exception exc) {
                    string msg = SimpleUtils.ExceptionMsg(exc, "FileSystemOps.FilterFSInfo.opts");
                    Console.WriteLine(msg);
                    myLog(msg);
                }
            }

            List<string> resultFinalList = new List<string>();

            DateTime dtStt = DateTime.Now;
            foreach (string nam in pars.Keys) {
                int rVal;
                try {
                    switch (nam) {
                        case "oper":
                            switch (pars[nam]) {
                                case "findf": {  // find files.  if destination than use it
                                        //consutil1 -v oper=findf src=c:\source  des=d:\destination
                                        rVal = TraverseAndCollect(opts, pars, resultFinalList, WalkDirectoryFileTree);
                                        if (pars.ContainsKey("des")) {
                                            if (rVal != 0) {
                                                string msg = SimpleUtils.ErrorMsg("aborting copy on error", rVal, "oper.copy walk");
                                                Console.WriteLine(msg);
                                                myLog(msg);
                                            }
                                            else {
                                                CopyFiles(opts, pars, resultFinalList);
                                            }
                                        }
                                    }
                                    break;

                                case "findd": { // find directories. sample to find directories with git repos, bare or not, in prep for duping directories or pulling for git updates.
                                        //consutil1 -v oper=invoked pat=hooks,.git patex=.git src=E:\_GIT_K_working_clones\  ttail=\ resultsf=\junk\K_Result.txt  destf=\junk\K_gitSrcDirs_pull.txt
                                        //consutil1 -v oper=findd pat=hooks,.git patex=.git src=E:\_GIT_K_working_clones\  ttail=\ resultsf=\junk\invoked_test_Result.txt cmd="C:\Program Files\Git\bin\git.exe" opts=pull  destf=\junk\test_gitSrcDirs_pull.txt workDir=@takesrc
                                        rVal = FindDirs(opts, pars, ref resultFinalList);
                                        if (pars.ContainsKey("ttail")) {
                                            List<string> templ = new List<string>();
                                            foreach (string dir in resultFinalList) {
                                                try {
                                                    templ.Add(dir.Substring(0, dir.LastIndexOf(pars["ttail"])));
                                                }
                                                catch (Exception exc) {
                                                    string lmsg = SimpleUtils.ExceptionMsg(exc," truncating dir for copy :" + dir);
                                                    Console.WriteLine(lmsg);
                                                    myLog(lmsg);
                                                }
                                            }
                                            resultFinalList = templ;
                                        }

                                        if (pars.ContainsKey("des")) {
                                            if (rVal != 0) {
                                                string msg = SimpleUtils.ErrorMsg("aborting copy on error", rVal, "oper.findd walk");
                                                Console.WriteLine(msg);
                                                myLog(msg);
                                            }
                                            else {
                                                CopyDirectories(args, opts, pars, resultFinalList);
                                            }
                                        }
                                    }
                                    break;
                                case "invoked": {
                                        // clone repos from a tree
                                        // -v oper=invoked pat=hooks,.git patex=.git src=E:\_GIT_K_working_clones\ des=g:\_t ttail=\ resultsf=\junk\K_e_Result.txt cmd="C:\Program Files\Git\bin\git.exe" opts=clone  destf=\junk\K_e_dest.txt workDir=@takesrc -fd=
                                        // pull a bunch of repos
                                        // consutil1 -v oper=invoked pat=hooks,.git patex=.git src=E:\_GIT_K_working_clones\  ttail=\ resultsf=\junk\invoked_G_Result.txt cmd="C:\Program Files\Git\bin\git.exe" opts=pull  destf=\junk\G_gitSrcDirs_pull.txt workDir=@takesrc
                                        rVal = FindDirs(opts, pars, ref resultFinalList);
                                        if (pars.ContainsKey("ttail")) {
                                            List<string> templ = new List<string>();
                                            foreach (string dir in resultFinalList) {
                                                try {
                                                    templ.Add(dir.Substring(0, dir.LastIndexOf(pars["ttail"])));
                                                }
                                                catch (Exception exc) {
                                                    string lmsg = SimpleUtils.ExceptionMsg(exc, " truncating dir for copy :" + dir);
                                                    Console.WriteLine(lmsg);
                                                    myLog(lmsg);
                                                }
                                            }
                                            resultFinalList = templ;
                                        }

                                        if (pars.ContainsKey("cmd")) {
                                            if (rVal != 0) {
                                                string msg = SimpleUtils.ErrorMsg("aborting copy on error", rVal, "oper.findd walk");
                                                Console.WriteLine(msg);
                                                myLog(msg);
                                            }
                                            else {
                                                InvokeDirectories(args, opts, pars, resultFinalList);
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
                    string msg = SimpleUtils.ExceptionMsg(exc, "FileSystemOps.FilterFSInfo.pars");
                    Console.WriteLine(msg);
                    myLog(msg);
                }
            }
            DateTime dtStp = DateTime.Now;
            Console.WriteLine("msecs=" + dtStp.Subtract(dtStt).TotalMilliseconds);

            if ((opts.ContainsKey("v")) && (opts["v"].Equals("1"))) {
                foreach (string stg in resultFinalList) {
                    Console.WriteLine(stg);
                }
                Console.WriteLine("List end");
            }
            if (pars.ContainsKey("resultsf")) {
                StreamWriter sw = new StreamWriter(pars["resultsf"]);
                foreach (string stg in resultFinalList) {
                    sw.WriteLine(stg);
                }
                sw.Close();
            }

            return resultFinalList;
        }

        private void CopyDirectories(string[] args, Dictionary<string, string> opts, Dictionary<string, string> pars, List<string> resultFinalList) {
            Parallel.ForEach(resultFinalList, dir => {
                Dictionary<string, string> lpars = pars.Keys.ToDictionary(par => par, par => pars[par]);
                lpars["oper"] = "findf";
                lpars["src"] = dir;
                try {
                    FileSystemOps fso = new FileSystemOps(myLog);
                    fso.FilterFSInfo(args, opts, lpars);
                }
                catch (Exception exc) {
                    string emsg = SimpleUtils.ExceptionMsg(exc, "CopyDirectories parallel exception at "+ dir);
                    Console.WriteLine(emsg);
                    myLog(emsg);
                }
            });


            string msg = SimpleUtils.InfoMsg("findd with des length ", resultFinalList.Count, "oper.findd walk");
            Console.WriteLine(msg);
            myLog(msg);
        }

        private int FindDirs(Dictionary<string,string> opts, Dictionary<string,string> pars, ref List<string> resultFinalList) {
            // example:  find git repos  (.git, or hooks not preceeded by .git)  consutil1 -v=1 oper=findd src=k:\_git\ pat=hooks,.git patex=.git
            List<string> srcDirsList = new List<string>();
            var rVal = TraverseAndCollect(opts, pars, srcDirsList, WalkDirectoryTree);
            if (rVal != 0) {
                string msg = SimpleUtils.ErrorMsg("aborting copy on error", rVal, "oper.copy walk");
                Console.WriteLine(msg);
                myLog(msg);
            }
            else {
                List<string> desDirsList = new List<string>();
                rVal = DedupList(opts, pars, srcDirsList, desDirsList); // dedupped dirs list
                Console.WriteLine("Directory List length:" + desDirsList.Count);
                if (pars.ContainsKey("pat")) {
                    string pattern = pars["pat"];
                    string[] pats = pattern.Split(',');
                    foreach (string pat in pats) {
                        if (pat.Length > 0) {
                            foreach (string dir in desDirsList) {
                                if (dir.EndsWith(pat)) {
                                    if (pars.ContainsKey("patex")) {  // todo: this should be pipelined not hardcoded 
                                        string patternExclude = pars["patex"];
                                        if (dir.IndexOf("\\") > 1) {
                                            if (dir.Substring(0, dir.LastIndexOf("\\")).IndexOf(patternExclude) > 0) {
                                                continue; // has a parent dir component that excludes this match
                                            }
                                        }
                                    }
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
            return rVal;
        }


        int TraverseAndCollect(Dictionary<string,string> opts, Dictionary<string,string> pars, List<string> srcFilesList, Walker walkMethod) {
            if (!pars.ContainsKey("src")) DoLogError("need [src] directory ");
            if (lastErr != 0) return lastErr;
            string src = pars["src"];

            if (!Directory.Exists(src)) DoLogError("[src] directory does not exist");
            if (lastErr != 0) return lastErr;

            DirectoryInfo disrc = new DirectoryInfo(src);
            walkMethod(disrc, srcFilesList);
            Console.WriteLine("found:" + srcFilesList.Count);
            return lastErr;
        }


        int InvokeDirectories(string[] args, Dictionary<string, string> opts, Dictionary<string, string> pars, List<string> srcFilesList) {
            List<string> desFilesList = new List<string>(); // Accumulate src/des pairs so can parallel copies
            char sdSepChar = '|';
            string des = null;
            if (pars.ContainsKey("des")) {
                des = pars["des"];
            }
            foreach (var sName in srcFilesList) {  // accumulate into a list that can remain unaltered during a parallel
                FileInfo sfi = new FileInfo(sName);
                string srcPath = sfi.DirectoryName;
                string dPath = (des != null)?Path.Combine(des, srcPath.Substring(3)):"";  // might not have a destination, but still want a list of sources
                string dName = (des != null) ? Path.Combine(dPath, sfi.Name) : "";
                desFilesList.Add(sName + sdSepChar + dName);
            }


            if (pars.ContainsKey("destf")) {
                StreamWriter sw = new StreamWriter(pars["destf"]);
                foreach (string stg in desFilesList) {
                    string[] sdPair = stg.Split(sdSepChar);
                    sw.WriteLine(sdPair[1]);
                }
                sw.Close();
            }
            if (pars.ContainsKey("nocmd")) {
                return 0;
            }

            string workDir = null;
            if (pars.ContainsKey("workDir")) {
                workDir = pars["workDir"];
            }


            ParallelLoopResult result = Parallel.ForEach(desFilesList, n => {
                ProcessStartInfo psi = new ProcessStartInfo();
                string[] sdPair = n.Split(sdSepChar);
                try {
                    if (workDir != null) {
                        switch (workDir) {
                            case "@takesrc":
                                psi.WorkingDirectory = sdPair[0];
                                sdPair[0] = "";
                                break;
                            case "@takedes":
                                psi.WorkingDirectory = sdPair[1];
                                sdPair[1] = "";
                                break;
                            case "src":
                                psi.WorkingDirectory = sdPair[0];
                                break;
                            case "des":
                                psi.WorkingDirectory = sdPair[1];
                                break;
                        }
                    }
                    psi.FileName = (pars.ContainsKey("cmd")) ? pars["cmd"] : "needCmdParToExecute.exe";
                    string copts = (pars.ContainsKey("opts")) ? pars["opts"] : "";
                    string cmdOpts = $"{copts} {sdPair[0]} {sdPair[1]}";

                    psi.Arguments = cmdOpts;

                    Process.Start(psi);
                }
                catch (Exception exc) {
                    string psiTxt = string.Format("{ psi file='{0}', Args='{1}'",psi.FileName, psi.Arguments);
                    string emsg = SimpleUtils.ExceptionMsg(exc, "parallel InvokeDirectories: "+ psiTxt +" at " + n);
                    Console.WriteLine(emsg);
                    myLog(emsg);
                }

            });
            myLog(SimpleUtils.InfoMsg("parallel copy result", (result.IsCompleted) ? 0 : 1, "oper.copy walk"));
            return lastErr;
        }


        int CopyFiles(Dictionary<string, string> opts, Dictionary<string, string> pars, List<string> srcFilesList) {
            // Replicate a given a list of files to a destination directory
            // e.g. :  consutil1 -v oper=findf src=c:\junk des=g:\junk\_des\d1\
            if (ValidateDestinationDir(opts, pars) != 0) return lastErr;

            string des = pars["des"];
            List<string> desFilesList = new List<string>();  // Accumulate src/des pairs so can parallel copies
            Hashtable directoryNames = new Hashtable();  // to cut down on probes for create
            char sdSepChar = '|';
            foreach (var sName in srcFilesList) {
                try {
                    FileInfo sfi = new FileInfo(sName);
                    string srcPath = sfi.DirectoryName;
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
                catch (Exception exc) {
                    string emsg = SimpleUtils.ExceptionMsg(exc, "dirs for CopyFiles at " + sName);
                    Console.WriteLine(emsg);
                    myLog(emsg);

                }
            }
            ParallelLoopResult result = Parallel.ForEach(desFilesList, n => {
                try {
                    string[] sdPair = n.Split(sdSepChar);
                    File.Copy(sdPair[0], sdPair[1]);
                }
                catch (Exception exc) {
                    string emsg = SimpleUtils.ExceptionMsg(exc, "parallel CopyFiles at " + n);
                    Console.WriteLine(emsg);
                    myLog(emsg);
                }
            });
            myLog(SimpleUtils.InfoMsg("parallel copy result", (result.IsCompleted) ? 0 : 1, "oper.copy walk"));
            return lastErr;
        }

        private int ValidateDestinationDir(Dictionary<string,string> opts, Dictionary<string,string> pars) {
            if (!pars.ContainsKey("des")) DoLogError("need [des] directory ");
            if (lastErr != 0) return lastErr;

            if (Directory.Exists(pars["des"])) DoLogError("[des] directory exists already", 0);
            if (lastErr != 0) return lastErr;

            string forceCopyRoot = (opts.ContainsKey("fd")) ? opts["fd"] : @"g:\junk\_des";
            if (forceCopyRoot.Length > 0) {
                if (pars["des"].IndexOf(forceCopyRoot) != 0) {
                    DoLogError(" requires [des] root as :" + forceCopyRoot, 2);
                }
            }
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


        int DedupList(Dictionary<string,string> opts, Dictionary<string,string> pars, List<string> srcFilesList, List<string> desDirsList) {
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

        void WalkDirectoryTree(DirectoryInfo root, List<string> dirsList) {
            dirsList.Add(root.FullName);
            foreach (DirectoryInfo dirInfo in root.GetDirectories()) {
                WalkDirectoryTree(dirInfo, dirsList);
            }
        }

        void WalkDirectoryFileTree(DirectoryInfo root, List<string> filesList) {
            FileInfo[] files = null;

            try {
                files = root.GetFiles("*.*");
            }
            catch (UnauthorizedAccessException e) {
                DoLogError(SimpleUtils.ExceptionMsg(e, "WalkDirectoryFileTree UnauthorizedAccessException"));
            }

            catch (DirectoryNotFoundException e) {
                Console.WriteLine(e.Message);
                DoLogError(SimpleUtils.ExceptionMsg(e, "WalkDirectoryFileTree DirectoryNotFoundException"));
            }

            catch (Exception e) {
                Console.WriteLine(e.Message);
                DoLogError(SimpleUtils.ExceptionMsg(e, "WalkDirectoryFileTree Exception"));
            }

            if (files != null) {
                filesList.AddRange(files.Select(fi => fi.FullName));
                DirectoryInfo[] subDirs = root.GetDirectories();

                foreach (DirectoryInfo dirInfo in subDirs) {
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
