using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Dac.Extensions.Prototype;
using Microsoft.SqlServer.Dac.Model;
using System.Resources;
using System.IO;
using System.Reflection;


namespace hgr.SqlServerTools
{

    public enum DiagramFormat
    {
        GraphViz,
        PlantUML
    };

    public class SqlDacFxDiagram
    {

        private TSqlTypedModel model;
        private StringBuilder diagram;
        private DiagramFormat format;




        public SqlDacFxDiagram(string pathToDacPac) : this(pathToDacPac,DiagramFormat.GraphViz)
        {
        }

        public SqlDacFxDiagram(string pathToDacPac, DiagramFormat diagramFormat)
        {
            model = new TSqlTypedModel(pathToDacPac);
            diagram = new StringBuilder();
            format = diagramFormat;


            if (format == DiagramFormat.GraphViz)
            {
                // Add the file header
                diagram.Append(Resources.GraphVizHeader);

                OutputDiagramSchemaDef();
                OutputDiagramRelationships();

                // Add the file footer
                diagram.Append(Resources.GraphVizFooter);
            }
            else if (format == DiagramFormat.PlantUML)
            {
                // Add the file header
                diagram.Append(Resources.PlantUMLHeader);

                // Add the file footer
                diagram.Append(Resources.PlantUMLFooter);
            }
        }


        public string Diagram
        {
            get
            {
                return diagram.ToString();
            }
        }

        /// <summary>
        /// Checks to see if there are any foreign keys defined on user objects. We can use this in calling app
        /// to flag a message that the diagram may be generated without any relationships as there are no
        /// foreign keys to infer them from
        /// </summary>
        public bool CheckModel()
        {
            var rels = model.GetObjects<TSqlForeignKeyConstraint>(DacQueryScopes.UserDefined);
            var strOut = new StringBuilder();

            if (rels.Count() == 0)
                return false;
            else
                return true;
        }


        /// <summary>
        /// Removes square bracket qualifiers from object names
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        private static string removeQualifiers(string inputString)
        {
            return (inputString.Replace("[", "").Replace("]", ""));
        }


        /// <summary>
        /// Loops over all 
        /// </summary>
        /// <param name="model"></param>
        /// <param name="outputFormat"></param>
        /// <returns></returns>
        private void OutputDiagramSchemaDef()
        {
            var schemas = model.GetObjects<TSqlSchema>(DacQueryScopes.Default);
            //StringBuilder outputstring = new StringBuilder();

            foreach (var schema in schemas)
            {
                if (schema.Name.Parts[0] == "dbo")
                {

                    if (format == DiagramFormat.PlantUML)
                    {
                        diagram.AppendFormat("\npackage {0} {{", schema.Name);
                    }

                    // put tables here
                    var x = schema.GetChildren(DacQueryScopes.UserDefined);


                    foreach (var thing in x)
                    {
                        if (thing.ObjectType == ModelSchema.Table)
                        {
                            var tbl = new TSqlTable(thing);
                            diagram.Append("\n");
                            OutputDiagramTableDef(tbl);
                            diagram.Append("\n");
                        }
                    }

                    //outputstring.Append("}\n");
                }
            }

            schemas = model.GetObjects<TSqlSchema>(DacQueryScopes.UserDefined);
            foreach (var schema in schemas)
            {
                diagram.AppendFormat("\npackage {0} {{", schema.Name);

                // put tables here
                var x = schema.GetChildren(DacQueryScopes.UserDefined);


                foreach (var thing in x)
                {
                    if (thing.ObjectType == ModelSchema.Table)
                    {
                        var tbl = new TSqlTable(thing);
                        diagram.Append("\n");
                        OutputDiagramTableDef(tbl);
                        diagram.Append("\n");
                    }
                }

                diagram.Append("\n}}");

            }

        }


        /// <summary>
        /// Outputs a table
        /// </summary>
        /// <param name="tbl"></param>
        /// <param name="outputFormat"></param>
        /// <returns></returns>
        private void OutputDiagramTableDef(TSqlTable tbl)
        {
            StringBuilder outputString = new StringBuilder();

            if (format == DiagramFormat.PlantUML)
            {
                // need to string [] from the name
                diagram.AppendFormat("table({0}) {{\n", removeQualifiers(tbl.Name.ToString()));
            }
            else if (format == DiagramFormat.GraphViz)
            {
                diagram.AppendFormat("{0} [label=<\n", removeQualifiers(tbl.Name.ToString()).Replace(".", "_"));
                diagram.AppendLine("\t<table border=\"0\" cellborder=\"1\" cellspacing=\"0\" cellpadding=\"4\">");
                diagram.AppendFormat("\t\t<tr><td bgcolor=\"lightblue\">{0}</td></tr>\n", removeQualifiers(tbl.Name.ToString()));
            }

            OutputDiagramColumns(tbl);

            if (format == DiagramFormat.PlantUML)
            {
                outputString.AppendLine("}\n");
                outputString.AppendLine("");
            }
            else if (format == DiagramFormat.GraphViz)
            {
                outputString.AppendLine("\t</table>");
                outputString.AppendLine(">]\n");
                outputString.AppendLine("");
            }
        }



        /// <summary>
        /// Outputs the columns of a table 
        /// </summary>
        /// <param name="outputString"></param>
        /// <param name="t"></param>
        /// <param name="outputFormat"></param>
        /// <returns></returns>
        private  void  OutputDiagramColumns(TSqlTable t)
        {
            foreach (var Column in t.Columns)
            {

                if (format == DiagramFormat.PlantUML)
                {
                    diagram.AppendFormat("\t{0}:", Column.Name.Parts[2]);
                }
                else if (format == DiagramFormat.GraphViz)
                {
                    diagram.AppendFormat("\t\t<tr><td align=\"left\">{0}:", Column.Name.Parts[2]);
                }

                foreach (var columnDataType in Column.DataType)
                {
                    if (format == DiagramFormat.PlantUML)
                    {
                        diagram.AppendFormat(" {0}", removeQualifiers(columnDataType.Name.ToString()));
                    }
                    else if (format == DiagramFormat.GraphViz)
                    {
                        diagram.AppendFormat("{0}", removeQualifiers(columnDataType.Name.ToString()));
                    }
                }

                //Check to see if the column is a PK
                foreach (var pk in t.PrimaryKeyConstraints)
                {
                    foreach (var primaryKeyColumn in pk.Columns)
                    {
                        if (primaryKeyColumn.Name.Parts[2] == Column.Name.Parts[2])
                        {
                            if (format == DiagramFormat.PlantUML)
                            {
                                diagram.AppendFormat("<<PK>>");
                            }
                            else if (format == DiagramFormat.GraphViz)
                            {
                                diagram.Append("(PK)");
                            }
                        }
                    }
                }

                // Check to see if the column is a FK
                foreach (var fk in t.ForeignKeyConstraints)
                {
                    foreach (var foreignKeyColumn in fk.Columns)
                    {
                        if (foreignKeyColumn.Name.Parts[2] == Column.Name.Parts[2])
                        {
                            if (format == DiagramFormat.PlantUML)
                            {
                                diagram.AppendFormat("<<FK>>");
                            }
                            else if (format == DiagramFormat.GraphViz)
                            {
                                diagram.Append("(FK)");
                            }
                        }
                    }
                }

                if (format == DiagramFormat.PlantUML)
                {
                    diagram.AppendFormat("\n");
                }
                else if (format == DiagramFormat.GraphViz)
                {
                    diagram.AppendFormat("</td></tr>\n");
                }

            }
        }


        /// <summary>
        /// Outputs the relationships using the foreign keys in the model
        /// </summary>
        private void OutputDiagramRelationships()
        {
            var rels = model.GetObjects<TSqlForeignKeyConstraint>(DacQueryScopes.UserDefined);
            var strOut = new StringBuilder();

            foreach (var rel in rels)
            {
                foreach (var ft in rel.ForeignTable)
                {
                    if (format == DiagramFormat.PlantUML)
                    {
                        diagram.AppendFormat("{0}", removeQualifiers(rel.GetParent().Name.ToString()).Replace(".", "_"));
                        diagram.AppendFormat(" -|> {0}:FK", removeQualifiers(ft.Name.ToString()));
                    }
                    else if (format == DiagramFormat.GraphViz)
                    {
                        diagram.AppendFormat("{0}->{1};", removeQualifiers(ft.Name.ToString()).Replace(".", "_"), removeQualifiers(rel.GetParent().Name.ToString()).Replace(".", "_"));
                    }

                }

            }
        }


    }
}
