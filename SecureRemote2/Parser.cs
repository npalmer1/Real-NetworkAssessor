using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;

namespace SecureRemote2
{
    
    class Parser
    {
        public string[] infile = new string[] { "", "", "", "", "", "", "", "", "", "" };
        public string[] ftemplate = new string[] { "", "", "", "", "", "", "", "", "", "" };
        public string fout, fcomment, umfout;
        static int maxtasks = 200;
        public int totaltasks = 0;
        static int maxPCs = 254;
        static int maxCriteria = 8;
        public string[] Criteria = new string[maxCriteria];
        public bool upCase = false;
                 

        public struct tasks
        {
            public bool taskexist;
            public int tasktotal;
            public int taskmax;
        }
       
        public tasks[] tasklist = new tasks[maxtasks];

        static int maxlines = 2000;
        public int defaultMark = 1;
        public bool[] lineused = new bool[maxlines];
        public string rawResult;
        

        public double MarkText() //look at tasklist and find total marks for each task
        {
            double max = 0.0;
            double total = 0.0;
            double result = 0.0;          

            rawResult = "";

            tasklist[0].taskexist = true;

            for (int i = 0; i <= totaltasks; i++)
            {
                if (i == 0 && !tasklist[i].taskexist) //if nothing found so no tasks
                {
                    //break;
                }
                if (tasklist[i].taskexist)
                {
                    max = max + tasklist[i].taskmax; //max number of tasks 
                    total = total + tasklist[i].tasktotal; //task total
                }
            }
            if (total > max || max == 0)
            {
                return 0.0;
            }
            else
            {
                result = (total/max) * 100;
                result = Math.Round(result,1); //%
               
                rawResult = Convert.ToString(Math.Round(total, 1)) + "/" + Convert.ToString(Math.Round(max, 1));
                return result; //overall score from total score divided by max possible score as %
            }
        }

        private void ClearTasks()
        {
            for (int i = 0; i < maxtasks; i++)
            {
                tasklist[i].taskexist = false;
                tasklist[i].tasktotal = 0;
                tasklist[i].taskmax = 0;
            }
        }
        private void ClearLines() //lines m,arked when used - so clear them before marking
        {
            for (int i = 0; i < maxlines; i++)
            {
                lineused[i] = false;
            }
        }
        
        public int Parse3(int fileno, bool append, bool checkdefault) //find number of lines correct in a file (infile) compared to template (ftemplate)
        {
            //string fname = "";
            //string ftemp = @"C:\output.txt";
            int linecorrect = 0;

            int inc = 0;
            string line = "";
            string l2 = "";
            string cfgcmd = "";
            string task = "";
            string tmptsk = "";
            string altstr = "";
            string altend = "";

            int mrk = 0;

            string commt = "";
            string mrkstr = "";
            string exc = "!";
            bool nomrk = true;
            int lineno = 0;

            int taskno = 0;

            ClearTasks();
            ClearLines();
            totaltasks = 0;
            bool found = false;
            bool wild = false; //**?
            bool exactwild = false; //***
            bool startwild = false; //**#
            string endstr = "";
            bool wildfound = false;
            bool first = true;
            bool alt = false;
            bool altwild = false;
            string sq = "";
            bool ends = false;
            bool hashcomment = false;


            try
            {
                if (!File.Exists(ftemplate[fileno]))
                {
                    return -1; //template file not found
                }
                alt = false;
                ends = false;
                //string ext = Path.GetExtension(fout);
                //string umfout = Path.GetFileNameWithoutExtension(fout);
                //string umpath = Path.GetDirectoryName(fout);
                //umfout = umpath + "\\"+ umfout + ".UM";
                using (StreamWriter outp = new StreamWriter(fout, append)) //open output file for marks
                {
                    using (StreamWriter umoutp = new StreamWriter(umfout, append)) //open output file for marks for import to Ultramarker
                    {
                        using (StreamWriter comment = new StreamWriter(fcomment, append)) //open output file for comments
                        {

                            using (StreamReader sw = new StreamReader(ftemplate[fileno])) //open template file for reading
                            {
                                if (Criteria[fileno] == "" || Criteria[fileno] == null)
                                {
                                    Criteria[fileno] = "1"; //set default criteria to 1
                                }
                                outp.WriteLine("Criteria: " + (fileno + 1).ToString());
                                outp.WriteLine("Marked file: " + infile[fileno]);
                                comment.WriteLine("Criteria: " + (fileno + 1).ToString());
                                comment.WriteLine("Marked file: " + infile[fileno]);
                                umoutp.WriteLine("Criteria: " + (fileno + 1).ToString());
                                umoutp.WriteLine("Marked file: " + infile[fileno]);
                                task = "0";
                                taskno = 0;

                                while (!sw.EndOfStream) //keep reading lines from template file until end
                                {
                                    alt = false;
                                    line = sw.ReadLine().Trim();  //read line from template file
                                    if (line.StartsWith("#") || line.StartsWith("# ")  || line.StartsWith("  #") )  //is it a comment? - don't mark it, but write it as a comment
                                    {
                                        hashcomment = true;
                                        string strt = "";
                                        string strt2 = "";
                                        string[] words = line.Split('■');
                                        
                                        if (words.Length > 1)
                                        {
                                            if (words[1].Contains("T"))
                                            {
                                                strt2 = words[1];
                                                strt2 = strt2.Replace("T", " - Task ");
                                            }
                                            else if (words.Length >2)
                                            {
                                                strt2 = words[2];
                                                strt2 = strt2.Replace("T", " - Task ");
                                            }
                                            strt = words[0];
                                            strt2 = words[0] + strt2;                                           
                                        }
                                        else
                                        {
                                            strt = line;
                                            strt2 = line + " - Task 0";
                                        }
                                        
                                        // using (StreamReader nw = new StreamReader(infile[fileno]))
                                        //its a comment
                                        if (File.Exists(infile[fileno]))
                                        {
                                            outp.WriteLine("Comment: " + strt);
                                            umoutp.WriteLine("Comment: " + strt2);
                                        }
                                        line = ""; //stop being re-written
                                    }
                                    else
                                    {
                                        hashcomment = false;
                                    }
                                    if (line.Contains("■") && line.Contains("NOTES:"))
                                    {
                                        ends = true;    //end of marked lines - now comments only
                                        umoutp.WriteLine("+++");
                                        umoutp.WriteLine(line);
                                    }
                                    else 
                                    {
                                        if (!ends && !hashcomment)
                                        {
                                            if (line.Contains("%ALT%")) //if there is an alternative command line
                                            {
                                                //extract all after %ALT% - whihc is the alternative command
                                                altstr = line.Substring(line.IndexOf("%ALT%" + 5)); //altstr contains alternative commands
                                                line = line.Substring(0, line.IndexOf("%ALT%"));  //beginning of line without alt line
                                                alt = true;
                                            }

                                            nomrk = true;
                                            task = "0";
                                            mrk = 0;
                                            sq = "";
                                            mrkstr = "";
                                            string[] words = line.Split('■'); // alt 254 special char 
                                            if (words.Length > 0 && line.Trim().Length > 0)
                                            {
                                                string mt = "";
                                                if (words.Length > 1)
                                                {
                                                    mt = words[1].ToUpper();
                                                }
                                                if (mt.Contains("T"))   //if we have ■T
                                                {
                                                    task = mt;
                                                }
                                                else if (words.Length > 2)
                                                {
                                                    task = words[2].ToUpper();  //if we have ■M ■T
                                                }
                                               
                                                     //task number - can be put at end of line
                                                    if (task.Contains("T"))
                                                    {
                                                        try
                                                        {
                                                            if (task.Contains("TA") || task.Contains("TB")) // TA and TB are for alternatives in a task
                                                            {   //for future expansion
                                                                task.Substring(task.IndexOf("T") + 2).Trim();
                                                            }
                                                            else
                                                            {
                                                                task = task.Substring(task.IndexOf("T") + 1).Trim();
                                                            }
                                                            
                                                            taskno = Convert.ToInt32(task);
                                                            if (taskno < maxtasks)
                                                            {
                                                                tasklist[taskno].taskexist = true;
                                                                if (taskno > totaltasks) { totaltasks = taskno; }
                                                            }
                                                        }
                                                        catch { }
                                                    }
                                                
                                                cfgcmd = words[0]; //this is the command read from the template for comaparison

                                                if (words.Length > 1)
                                                {

                                                   
                                                    sq = "";
                                                    mrkstr = "";
                                                  
                                                    mrkstr = words[1].ToUpper(); //mark for this configuration command   
                                                    
                                                    if (words.Length > 1 && mrkstr.Contains("M"))
                                                    {
                                                        sq = " ■";
                                                        try
                                                        {
                                                            mrk = 0;
                                                            string mrkN = "";
                                                            mrkN = mrkstr.Substring(mrkstr.IndexOf("M") + 1).Trim();
                                                            mrk = Convert.ToInt32(mrkN);
                                                            if (taskno < maxtasks && !checkdefault) //if task less than max and default override not checked
                                                            {
                                                                tasklist[taskno].taskmax = mrk + tasklist[taskno].taskmax; //mark this if a mark found in template
                                                            }   //default mark overrides this
                                                            nomrk = false;

                                                        }
                                                        catch { nomrk = true; }
                                                    }
                                                    else
                                                    {
                                                        sq = "";
                                                        mrkstr = "";
                                                    }


                                                }

                                                line = line.Trim();
                                                if (line != exc) //if line isn't an ! on its own
                                                {
                                                    if (checkdefault || nomrk)   //use default mark if check box checked to override marks
                                                    {
                                                        mrk = defaultMark;
                                                        tasklist[taskno].taskmax = mrk + tasklist[taskno].taskmax;
                                                    }
                                                }

                                                if (words.Length > 2)
                                                {
                                                    commt = words[2]; //comment for feedback
                                                }

                                                lineno = 0;
                                                if (line.Length > 0 && line != exc && !hashcomment)
                                                {
                                                    string cfgstr = "";
                                                    if (File.Exists(infile[fileno]))
                                                    {

                                                        //scroll through the input files looking for a match for the config commmand in the template file
                                                        using (StreamReader nw = new StreamReader(infile[fileno]))
                                                        {

                                                            found = false;
                                                            inc = 0;    //if command line contains wildcards
                                                            if (cfgcmd.Contains("**?")) //if **? is wildcard then mark line in two parts
                                                            {
                                                                wild = true;

                                                            }
                                                            else
                                                            {
                                                                wild = false;
                                                            }
                                                            if (cfgcmd.Contains("***")) //if *** is wildcard then whole line must be correct
                                                            {
                                                                exactwild = true;
                                                            }
                                                            else
                                                            {
                                                                exactwild = false;
                                                            }
                                                            if (cfgcmd.Contains("**#")) //if **# is wildcard then only first part of line need be correct
                                                            {
                                                                startwild = true;

                                                            }
                                                            else
                                                            {
                                                                startwild = false;
                                                            }
                                                            if (wild || exactwild || startwild)
                                                            {
                                                                endstr = cfgcmd.Substring(cfgcmd.IndexOf("**") + 3).Trim(); //end of string after wildcard
                                                                cfgcmd = cfgcmd.Substring(0, cfgcmd.IndexOf("**")); // string up to wildcard
                                                            }
                                                            else
                                                            {
                                                                endstr = "";
                                                            }
                                                            wildfound = false;
                                                            if (altstr.Contains("**A")) //if alternative command has a wildcard
                                                            {
                                                                altstr = altstr.Substring(0, altstr.IndexOf("**A"));
                                                                altend = altstr.Substring(altstr.IndexOf("**A" + 3));
                                                                altwild = true;
                                                            }
                                                            else
                                                            {
                                                                altend = "";
                                                            }

                                                            if (!ends)
                                                            {

                                                                while (!nw.EndOfStream) //while not end of input file
                                                                {

                                                                    l2 = nw.ReadLine(); //keep reading from input file (until end)
                                                                    l2 = l2.Trim();

                                                                    lineno++;
                                                                    if (inc < 1)
                                                                    {
                                                                        if (!lineused[lineno - 1])
                                                                        {
                                                                            bool contains = false;
                                                                            if (altstr.StartsWith("*+*"))
                                                                            {
                                                                                contains = true;
                                                                                altstr = altstr.Replace("*+*", "");
                                                                            }
                                                                            string line2 = l2;
                                                                            if (upCase)
                                                                            {
                                                                                line2 = line2.ToUpper();
                                                                                altstr = altstr.ToUpper();
                                                                            }
                                                                            if ((line2.StartsWith(altstr.Trim()) || line2.Contains(altstr.Trim()) && contains) && line2.Contains(altend) && altstr.Trim().Length > 0) //if alternative command found
                                                                            {
                                                                                lineused[lineno - 1] = true; //mark line as found - to reduce effect of duplication of commands                                                                                                                               
                                                                                if (taskno == 0)
                                                                                {
                                                                                    tasklist[taskno].taskexist = true;
                                                                                }
                                                                                linecorrect++; //counts number of correct lines
                                                                                inc++;
                                                                                break; //ensure that the next lines are not procesed
                                                                            }
                                                                            //if (l2.StartsWith(cfgcmd.Trim()) || (startwild && l2.StartsWith(endstr.Trim())) )   //if the file to be marked contains the command then output it to the file
                                                                            cfgstr = cfgcmd.Trim();
                                                                            contains = false;
                                                                            if (cfgstr.StartsWith("*+*"))
                                                                            {
                                                                                contains = true;
                                                                                cfgstr = cfgstr.Replace("*+*","");
                                                                            }
                                                                            string endstr1 = endstr.Trim();
                                                                            if (upCase)
                                                                            {
                                                                                cfgstr = cfgstr.ToUpper().Trim();
                                                                                endstr1 = endstr1.ToUpper().Trim();
                                                                            }
                                                                            if ((line2.StartsWith(cfgstr) || line2.Contains(cfgstr) && contains) ||(startwild && line2.StartsWith(cfgstr) && line2.Contains(endstr1)))   //if the file to be marked contains the command then output it to the file
                                                                            {
                                                                                lineused[lineno - 1] = true; //mark line as found - to reduce effect of duplication of commands                                                                                                                               
                                                                                if (taskno == 0)
                                                                                {
                                                                                    tasklist[taskno].taskexist = true;
                                                                                }
                                                                                if (exactwild) //only mark whole line correct if second part correct too (***)
                                                                                {
                                                                                    if (line2.Contains(endstr1))
                                                                                    {
                                                                                        wild = false;

                                                                                        tasklist[taskno].tasktotal = tasklist[taskno].tasktotal + mrk; //add mark to total for task
                                                                                        linecorrect++; //counts number of correct lines
                                                                                        inc++;
                                                                                        wildfound = true;
                                                                                    }
                                                                                }
                                                                                else //either whole line or first part of line are correct (wildcard **#)
                                                                                {
                                                                                    tasklist[taskno].tasktotal = tasklist[taskno].tasktotal + mrk; //add mark to total for task
                                                                                    linecorrect++; //counts number of correct lines
                                                                                    inc++;

                                                                                }
                                                                                if (wild) //wildcard for second part of line - (**?) ,mark first and second parts separately
                                                                                {
                                                                                    tasklist[taskno].taskmax = mrk + tasklist[taskno].taskmax; //don't forget there's an extra mark!
                                                                                    if (line2.Contains(endstr1)) //line is treated as two parts with separate mark for each
                                                                                    {
                                                                                        tasklist[taskno].tasktotal = tasklist[taskno].tasktotal + mrk; //add mark to total for task
                                                                                        linecorrect++; //counts number of correct lines
                                                                                        inc++;
                                                                                        wildfound = true;
                                                                                    }
                                                                                }
                                                                                outp.Write("Task: " + task + ". ");
                                                                                umoutp.Write("Task: " + task + ". ");

                                                                                cfgstr = cfgcmd.Trim();
                                                                                cfgstr = cfgstr.Replace("*+*", "");
                                                                                if (wild || exactwild) //if **? or *** wildcard and first and second part found/not found
                                                                                {
                                                                                    if (wildfound)
                                                                                    {
                                                                                        outp.WriteLine("Command: " + cfgstr + " " + endstr);
                                                                                        umoutp.WriteLine("Command: " + cfgstr + " " + endstr + sq + mrkstr);
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        outp.WriteLine("Command: partially correct: " + cfgstr + " " + endstr);
                                                                                        umoutp.WriteLine("Command: partially correct: " + cfgstr + " " + endstr + sq + mrkstr);
                                                                                    }

                                                                                }
                                                                                else if (startwild)
                                                                                {
                                                                                    outp.WriteLine("Command: " + endstr);
                                                                                    umoutp.WriteLine("Command: " + endstr + sq + mrkstr);
                                                                                }
                                                                                else
                                                                                {
                                                                                    outp.WriteLine("Command: " + cfgstr);
                                                                                    umoutp.WriteLine("Command: " + cfgstr + sq + mrkstr);
                                                                                }
                                                                                found = true;
                                                                            }

                                                                        } //lineused

                                                                    }
                                                                } //if !ends


                                                            } //wend
                                                            if (!found && !ends)
                                                            {
                                                                cfgstr = cfgcmd.Replace("*+*", "");
                                                                outp.Write("Task: " + task + ". ");
                                                                outp.WriteLine("Command NOT found: " + cfgstr + endstr);
                                                                umoutp.Write("Task: " + task + ". ");
                                                                umoutp.WriteLine("Command NOT found: " + cfgstr + endstr + sq + mrkstr);
                                                            }

                                                            nw.Close();
                                                            if (commt.Trim().Length > 0 && !found && !ends && !hashcomment)
                                                            {
                                                                comment.WriteLine(commt); //comment for feedback if task not achieved
                                                                commt = "";
                                                                comment.Write("Task: " + task + ". ");
                                                                comment.WriteLine("Not found: " + cfgcmd);
                                                            }
                                                            /*if (hashcomment) //if its just a comment line don't mark it, but write it
                                                            {
                                                                outp.Write("Task: " + task + ". ");
                                                                outp.WriteLine("Comment: " + cfgcmd + endstr);
                                                                umoutp.Write("Task: " + task + ". ");
                                                                umoutp.WriteLine("Comment: " + cfgcmd + endstr);
                                                                comment.Write("Task: " + task + ". ");
                                                                comment.Write("Comment: " + cfgcmd + endstr);
                                                            }
                                                            */

                                                        }  //using infile
                                                    } //file exists
                                                    else
                                                    {
                                                        if (first) //only write this once if the file not found
                                                        {
                                                            outp.WriteLine("No matching configuration");
                                                            umoutp.WriteLine("No matching configuration");
                                                            first = false;
                                                        }
                                                    }

                                                } //if line

                                            } //if words.length >0
                                        } //if !ends
                                        else if (!hashcomment)
                                        {
                                            umoutp.WriteLine(line);
                                        }

                                    } //if not startswith a # (comment)


                                } //while not eof template
                                sw.Close();
                            } //using ftemplate
                            comment.Close();
                        } //using umoutp
                        umoutp.Close();
                    } //using fcomment
                    outp.Close();
                } //using fout

            } //tryured
            catch
            {
                DialogResult r = MessageBox.Show("Error occurred processing files");
            }
            return linecorrect;

        }

        public int SuggestTemplate(int fileno, bool append)
        {   //compares solution template to initial file and saves difference                  
            string line1 = "";
            string line2 = "";
            string file1 = "";
            string ext = "";
            int line = -1;
            string[] splits = new string[2];            
            int status = 0;
        
            try
            {
                
                if (!File.Exists(ftemplate[fileno]))
                {
                    return -1; //template file not found
                }
                    splits = ftemplate[fileno].Split('.');     
                if (splits.Count() > 1)
                {
                    ext = "." + splits[1];
                }
                else
                {
                    ext = "";
                }
                    file1 = splits[0] + "_t1" + ext;
             
                 //note in this case template is the solution config, infile is the original config
                using (StreamReader tw = new StreamReader(ftemplate[fileno]))
                {
                    using (StreamWriter outf = new StreamWriter(file1, false)) //open output file for marks
                    {
                        outf.WriteLine("Template file: " + file1);
                        while (!tw.EndOfStream)
                        {
                            line1 = tw.ReadLine();  //read line from template file
                            using (StreamReader iw = new StreamReader(infile[fileno])) //open template file for reading
                            {
                              
                                while (!iw.EndOfStream)
                                {


                                    line2 = iw.ReadLine();
                                    line++;
                                    //scroll through the input files looking for a match for the config commmand in the template file
                                    if (line1 == line2)
                                    {
                                        if (line1.Trim() != "!") //cisco comment
                                        {
                                            line1 = "###" + line1;
                                        }
                                        //outf.WriteLine(line1);                                       
                                        break;
                                    }
                                    

                                }
                                iw.Close();
                            }
                            outf.WriteLine(line1, true); //append
                        }                      
                        tw.Close();
                        outf.Close();
                    }
                    
                }
                

            } //try
            
           catch
            {
                DialogResult r = MessageBox.Show("Error occurred processing files");
                status = -1;
            }
            if (status == 0)
            {
                MessageBox.Show("Template files created successfully");
            }
            else
            {
                MessageBox.Show("Problems creating template files");
            }
            return status;

        }
    }
}
