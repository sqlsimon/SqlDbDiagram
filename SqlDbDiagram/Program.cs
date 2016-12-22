using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using hgr.SqlServerTools.SqlDbDiagram;
using CommandLine;
using CommandLine.Text;
using System.IO;

//TODO: create a test project for the DacFx diagram class that tests various dacpacs
//TODO: go and get adventureworks dacpac + other sql server publically available dacpacs

namespace hgr.SqlServerTools.SqlDbDiagram
{

    class Options
    {
        [Option('d', "dacpac", Required = true,
          HelpText = "dacpac file to generate diagram for ")]
        public string InputFile { get; set; }

        [Option('f', "format", DefaultValue = "GraphViz",
          HelpText = "the output format of the diagram")]
        public string Format { get; set; }

        [Option('o', "outputfile", DefaultValue = null,
          HelpText = "the output format of the diagram")]
        public string OutputFile { get; set; }

        [Option('g', "generate", DefaultValue = false,
        HelpText = "the output format of the diagram")]
        public bool Generate { get; set; }

        [Option('c', "configGVizPath", DefaultValue = @"C:\Program Files (x86)\Graphviz2.38\bin",
        HelpText = "the path to the GraphViz bin directory")]
        public string GVizPath { get; set; }

        [Option('p', "configPlantUMLPath", DefaultValue = @"C:\Program Files (x86)\PlantUML",
        HelpText = "the path to the PlantUML jar file")]
        public string PlantUMLPath { get; set; }

        //TODO: Need an option for GraphViz output format, PDF, PNG, SVG

        //TODO Need to check for Java being in the path

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }

        class Program
    {
        static void Main(string[] args)
        {

            var options = new Options();
            SqlDacFxDiagram diag;
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                 DiagramFormat format = DiagramFormat.GraphViz; 

                 if (options.InputFile != null)
                 {
                    // check that the file exists and throw a file not found exception
                    if (File.Exists(options.InputFile))
                    {

                        if (options.Format == "GraphViz")
                        {
                            format = DiagramFormat.GraphViz;
                        }
                        else if (options.Format == "PlantUML")
                        {
                            format = DiagramFormat.PlantUML;
                        }

                        if (options.Format != null)
                        {
                            diag = new SqlDacFxDiagram(options.InputFile, format);
                        
                        }
                        else
                        {
                            diag = new SqlDacFxDiagram(options.InputFile);
                        }

                        if (diag.CheckModel())
                            System.Console.WriteLine("WARNING: No foreign keys were found in the dac model. Relationships cannot be modelled. ");

                        // SPECIFY GRAPHVIZ OUTPUT FORMAT, PDF, PNG, SVG

                            // EMBED HYPERLINKS IN SVG TO ALLOW TO BE EMBEDDED IN HTML OUTPUT


                        if (options.OutputFile == null)
                        {
                            options.OutputFile = Path.GetFileNameWithoutExtension(options.InputFile) + ".svg";
                         }

                        if (options.Generate && format == DiagramFormat.GraphViz && !File.Exists(options.GVizPath + @"\dot.exe"))
                        {
                            string msg = String.Format("Unable to find Graphviz binaries in {0}", options.GVizPath);
                            throw new FileNotFoundException(msg);
                        }

                        if (options.Generate && format == DiagramFormat.PlantUML && !File.Exists(options.PlantUMLPath + @"\plantuml.jar"))
                        {
                            string msg = String.Format("Unable to find PlantUML jar file in {0}", options.PlantUMLPath);
                            throw new FileNotFoundException(msg);

                        }
                    }
                    else
                    {
                        string s = String.Format("The file '{0}' does not exist, please check that the file and path name are correct", options.InputFile);
                        throw new FileNotFoundException(@"[data.txt not in c:\temp directory]");
                    }

     
                    // we should do the same for plant uml too
                }
            }

                

  
        }
    }
}
