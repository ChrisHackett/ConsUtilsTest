using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace consutil1 {
    class interpret1 {
        static void Main(string[] args) {
            Operator1 oper = new Operator1();
            oper.Main(args);
        }
    }

    class Operator1 {

        public int Main(string[] args) {
            int rVal = 0;
            StringDictionary opts;
            StringDictionary pars;
            int argsErr = getOpts(args, out opts, out pars);
            try {
                switch (args[0]) {
                    case "help": Console.WriteLine("no help here");
                        break;
                }
            }
            catch (Exception exc) {
                rVal = exc.HResult;
                Console.WriteLine(errMsg(exc,"Operator1.Main"));
            }
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
                        throw new Exception(val);
                    }
                }
                catch (Exception exc) {
                    if (rVal == 0) rVal = exc.GetHashCode();
                    Console.WriteLine(errMsg(exc, stgVal));
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


    }
}
