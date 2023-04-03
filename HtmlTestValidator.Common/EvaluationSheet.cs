using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using HtmlTestValidator.Models;
using HtmlTestValidator.Models.Project;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HtmlTestValidator
{
    public class EvaluationSheet: IDisposable
    {

        public MemoryStream Stream { get; private set; } = null;
        public SpreadsheetDocument SpreadsheetDocument { get; private set; } = null;
        public Sheet DataSheet { get; private set; }
        
        private WorkbookPart workbookpart;
        private SheetData sheetData;
        private WorksheetPart worksheetPart;
        private Columns columns;

        public EvaluationSheet(Project project, List<Evaluation> evaluations)
        {
            this.CreateWorkBook();

            Header(project, evaluations);
            EvaluationRows(evaluations, project);
            Summary(evaluations, project);
            MergeCells(project);
            SetColumnWidths(project);
            AddConitionalFormat($"C5:{GetCellReference(5 + evaluations.Count, 4 + project.Steps.Length)}");

            

        }

        public void SaveAs(string path)
        {
            this.workbookpart.Workbook.Save();
            this.SpreadsheetDocument.Close();
            this.Stream.Seek(0, SeekOrigin.Begin);
            var array = this.Stream.ToArray();
            File.WriteAllBytes(path, array);

        }

        public void Dispose()
        {
            if (SpreadsheetDocument != null)
                SpreadsheetDocument.Dispose();
            if (Stream != null)
                Stream.Dispose();
        }

        private void CreateWorkBook()
        {
            Stream = new MemoryStream();
            this.SpreadsheetDocument = SpreadsheetDocument.Create(this.Stream, SpreadsheetDocumentType.Workbook);

            this.workbookpart = this.SpreadsheetDocument.AddWorkbookPart();
            this.workbookpart.Workbook = new Workbook();

            this.worksheetPart = this.workbookpart.AddNewPart<WorksheetPart>();
            this.sheetData = new SheetData();
            this.worksheetPart.Worksheet = new Worksheet(this.sheetData);

            Sheets sheets = this.SpreadsheetDocument.WorkbookPart.Workbook.AppendChild<Sheets>(new Sheets());

            this.DataSheet = new Sheet()
            {
                Id = this.SpreadsheetDocument.WorkbookPart.GetIdOfPart(this.worksheetPart),
                SheetId = 1,
                Name = "Adatok"
            };
            sheets.Append(DataSheet);
            AddStyleSheet();

            this.columns = worksheetPart.Worksheet.GetFirstChild<Columns>();
            
        }

        private void Header(Project project, List<Evaluation> evaluations)
        {
#region 1. sor
            sheetData.Append(
                new Row(
                    new Cell()
                    {
                        CellReference = "A1",
                        CellValue = new CellValue($"{project.Class}\r{project.Date}"),
                        DataType = CellValues.String,
                        StyleIndex = 3
                    },

                    new Cell()
                    {
                        CellReference = "B1",
                        CellValue = new CellValue("Jelen volt"),
                        DataType = CellValues.String,
                        StyleIndex = 2
                    },

                    new Cell()
                    {
                        CellReference = "C1",
                        CellValue = new CellValue("metaadatok megadása"),
                        DataType = CellValues.String,
                        StyleIndex = 5
                    },
                    new Cell()
                    {
                        CellReference = "D1",
                        CellValue = new CellValue(project.Name),
                        DataType = CellValues.String,
                        StyleIndex = 1
                    },
                    new Cell()
                    {
                        CellReference = GetCellReference(1, 4 + project.Steps.Length),
                        CellValue = new CellValue("Határidőre feltöltés"),
                        DataType = CellValues.String,
                        StyleIndex = 5
                    },
                    new Cell()
                    {
                        CellReference = GetCellReference(1, 5 + project.Steps.Length),
                        CellValue = new CellValue("+pontok / \r-pontok"),
                        DataType = CellValues.String,
                        StyleIndex = 5
                    },
                    new Cell()
                    {
                        CellReference = GetCellReference(1, 6 + project.Steps.Length),
                        CellValue = new CellValue("Átlag:"),
                        DataType = CellValues.String,
                        StyleIndex = 3
                    },
                    new Cell()
                    {
                        CellReference = GetCellReference(1, 8 + project.Steps.Length),
                        CellFormula = new CellFormula($"If(COUNT(5:{4 + evaluations.Count}), AVERAGE({GetCellReference(5, 8 + project.Steps.Length)}:{GetCellReference(4 + evaluations.Count, 8 + project.Steps.Length)}), \"-\")"),
                        DataType = CellValues.Number,
                        StyleIndex = 3
                    },
                    new Cell(
                        new InlineString(
                            new Run(new Text("Javító tanár neve:"))
                            {
                                RunProperties = new RunProperties(
                                    new FontSize() { Val = 12}                      
                                )
                            },
                            new Run(new Text($"\r{project.Teacher}") { Space = SpaceProcessingModeValues.Preserve})
                            {
                                RunProperties = new RunProperties(
                                    new Bold(),
                                    new FontSize() { Val = 18}
                                )                                
                            }
                        )
                    )
                    {
                        CellReference = GetCellReference(1, 9 + project.Steps.Length),
                        DataType = CellValues.InlineString,
                        StyleIndex= 9
                    }
                )
                {
                    RowIndex = 1 
                }
            );
#endregion 1. sor

#region 2. sor
            sheetData.Append(
                new Row(
                    project.Steps.Select((s, i) => new Cell()
                    { 
                        CellReference = GetCellReference(2, i + 4),
                        CellValue = new CellValue(s.Description),
                        DataType = CellValues.String,
                        StyleIndex = 4,
                    })
                )
                {
                    RowIndex = 2,
                    Height = 170
                }
            );
#endregion 2. sor

#region 3. sor
            sheetData.Append(
                new Row(
                    new List<Cell>()
                    {
                         new Cell()
                         {
                             CellReference = "C3",
                             CellValue = new CellValue(""),
                             DataType = CellValues.String,
                             StyleIndex = 7
                         }
                    }.Union(
                        project.Steps.Select((s, i) => new Cell()
                        {
                            CellReference = GetCellReference(3, i + 4),
                            CellValue = new CellValue(s.Id),
                            DataType = CellValues.String,
                            StyleIndex = 6,
                        })
                    ).Union (
                        new List<Cell>()
                        {
                             new Cell()
                             {
                                 CellReference = GetCellReference(3,  4 + project.Steps.Length),
                                 CellValue = new CellValue(""),
                                 DataType = CellValues.String,
                                 StyleIndex = 7
                             },
                             new Cell()
                             {
                                 CellReference = GetCellReference(3,  5 + project.Steps.Length),
                                 CellValue = new CellValue(""),
                                 DataType = CellValues.String,
                                 StyleIndex = 7
                             }

                        }
                    )
                )
                {
                    RowIndex = 3
                }
            );
#endregion 3. sor

#region 4. sor
            sheetData.Append(
                new Row(
                    new List<Cell>()
                    {
                         new Cell()
                         {
                             CellReference = "A4",
                             CellValue = new CellValue("Név"),
                             DataType = CellValues.String,
                             StyleIndex = 8
                         },
                         new Cell()
                         {
                             CellReference = "B4",
                             CellValue = new CellValue("ü"),
                             DataType = CellValues.String,
                             StyleIndex = 12
                         },
                         new Cell()
                         {
                             CellReference = "C4",
                             CellValue = new CellValue(0),
                             DataType = CellValues.Number,
                             StyleIndex = 11
                         },

                    }.Union(
                        project.Steps.Select((s, i) => new Cell()
                        {
                            CellReference = GetCellReference(4, i + 4),
                            CellValue = new CellValue(s.Points),
                            DataType = CellValues.Number,
                            StyleIndex = 10,
                        })
                    ).Union (
                        new List<Cell>()
                        {
                             new Cell()
                             {
                                 CellReference = GetCellReference(4,  4 + project.Steps.Length),
                                 CellValue = new CellValue("0"),
                                 DataType = CellValues.Number,
                                 StyleIndex = 11
                             },
                             new Cell()
                             {
                                 CellReference = GetCellReference(4,  5 + project.Steps.Length),
                                 CellValue = new CellValue(0),
                                 DataType = CellValues.Number,
                                 StyleIndex = 11
                             },
                             new Cell()
                             {
                                 CellReference = GetCellReference(4,  6 + project.Steps.Length),
                                 CellFormula = new CellFormula($"SUM(C4:{GetCellReference(4, 5 + project.Steps.Length)})"),
                                 DataType = CellValues.Number,
                                 StyleIndex = 10
                             },
                             new Cell()
                             {
                                 CellReference = GetCellReference(4,  7 + project.Steps.Length),
                                 CellValue = new CellValue(1),
                                 DataType = CellValues.Number,
                                 StyleIndex = 14
                             },
                             new Cell()
                             {
                                 CellReference = GetCellReference(4,  8 + project.Steps.Length),
                                 CellValue = new CellValue("Jegy"),
                                 DataType = CellValues.String,
                                 StyleIndex = 8
                             },
                             new Cell()
                             {
                                 CellReference = GetCellReference(4,  9 + project.Steps.Length),
                                 CellValue = new CellValue("Megjegyzések"),
                                 DataType = CellValues.String,
                                 StyleIndex = 15
                             }

                        }
                    )
                )
                {
                    RowIndex = 4
                }
            );
#endregion 4. sor

        }

        private void EvaluationRows(List<Evaluation> evaluations, Project project)
        {
            uint rowIndex = 5;
            foreach (var evaluation in evaluations)
            {
                sheetData.Append(
                    evaluation.StepErrors.Select((p, i) => new Comment() {
                        Reference = GetCellReference((int)rowIndex, i + 4),
                        AuthorId = 0,
                        CommentText = new CommentText(new Run()
                        {
                            Text = new Text(p ?? "")
                        })
                    }));
                sheetData.Append(
                   new Row(
                       new List<Cell>()
                       {
                             new Cell()
                             {
                                 CellReference = GetCellReference((int)rowIndex, 1),
                                 CellValue = new CellValue(evaluation.Name),
                                 DataType = CellValues.String,
                                 StyleIndex = 0
                             },
                             new Cell()
                             {
                                 CellReference = GetCellReference((int)rowIndex, 2),
                                 CellValue = evaluation.Presenced ? new CellValue( "ü") : null,
                                 DataType = CellValues.String,
                                 StyleIndex = 13
                             },
                             new Cell()
                             {
                                 CellReference = GetCellReference((int)rowIndex,  3),
                                 CellValue = new CellValue(""),
                                 DataType = CellValues.Number,
                                 StyleIndex = 9
                             },
                       }.Union(
                           evaluation.StepPoints.Select((p, i) => new Cell()
                           {
                               CellReference = GetCellReference((int)rowIndex, i + 4),
                               CellValue = new CellValue(evaluation.Presenced ? p.ToString() : ""),
                               DataType = CellValues.Number,
                               StyleIndex = 9,
                           })
                       ).Union(
                           new List<Cell>()
                           {
                                 new Cell()
                                 {
                                     CellReference = GetCellReference((int)rowIndex,  4 + project.Steps.Length),
                                     DataType = CellValues.Number,
                                     StyleIndex = 9
                                 },
                                 new Cell()
                                 {
                                     CellReference = GetCellReference((int)rowIndex,  5 + project.Steps.Length),
                                     DataType = CellValues.Number,
                                     StyleIndex = 9
                                 },
                                 new Cell()
                                 {
                                     CellReference = GetCellReference((int)rowIndex,  6 + project.Steps.Length),
                                     CellFormula = new CellFormula($"IF({GetCellReference((int)rowIndex, 2)}=\"ü\", SUM({GetCellReference((int)rowIndex, 3)}:{GetCellReference((int)rowIndex, 5 + project.Steps.Length)}), \"\")"),
                                     DataType = CellValues.Number,
                                     StyleIndex = 10,
                                 },
                                 new Cell()
                                 {
                                     CellReference = GetCellReference((int)rowIndex,  7 + project.Steps.Length),
                                     CellFormula = new CellFormula($"IF({GetCellReference((int)rowIndex, 2)}=\"ü\", {GetCellReference((int)rowIndex, 6 + project.Steps.Length)} / {GetCellReference(4, 6 + project.Steps.Length, true)}, \"\")"),
                                     DataType = CellValues.Number,
                                     StyleIndex = 14
                                 },
                                 new Cell()
                                 {
                                     CellReference = GetCellReference((int)rowIndex,  8 + project.Steps.Length),
                                     CellFormula = new CellFormula($"IF({GetCellReference((int)rowIndex, 2)}=\"ü\", VLOOKUP({GetCellReference((int)rowIndex,  7 + project.Steps.Length)}, {GetCellReference(8 + evaluations.Count, 6 + project.Steps.Length, true)}:{GetCellReference(12 + evaluations.Count, 8 + project.Steps.Length, true)}, 3, TRUE), \"\")"),
                                     DataType = CellValues.Number,
                                     StyleIndex = 8
                                 }
                           }
                       )
                   )
                   {
                       RowIndex = rowIndex++
                   }
               );
            }
        }

        private void Summary(List<Evaluation> evaluations, Project project)
        {
            sheetData.Append(
                new Row(
                    new List<Cell>()
                    {
                        new Cell()
                        {
                            CellReference = GetCellReference(5 + evaluations.Count, 1),
                            CellFormula = new CellFormula($"COUNTA({GetCellReference(5, 1)}:{GetCellReference(4 + evaluations.Count, 1)})"),
                            DataType = CellValues.Number,
                            StyleIndex = 17
                        },
                        new Cell()
                        {
                            CellReference = GetCellReference(5 + evaluations.Count, 2),
                            CellFormula = new CellFormula($"COUNTA({GetCellReference(5, 2)}:{GetCellReference(4 + evaluations.Count, 2)})"),
                            DataType = CellValues.Number,
                            StyleIndex = 17
                        }
                    }.Union(
                        evaluations.First().StepPoints.Select((p, i) => new Cell()
                        {
                            CellReference = GetCellReference(5 + evaluations.Count, i + 4),
                            CellFormula = new CellFormula($"COUNTIF({GetCellReference(5, i + 4)}:{GetCellReference(4 + evaluations.Count, i + 4)}, 1)"),
                            DataType = CellValues.Number,
                            StyleIndex = 17
                        })
                    )
                    )
                {
                    RowIndex = (uint)(5 + evaluations.Count)
                }

            );

            sheetData.Append(
                new Row(
                    new Cell()
                    {
                        CellReference = GetCellReference(7 + evaluations.Count, 7 + project.Steps.Length),
                        CellValue = new CellValue("Pont"),
                        DataType = CellValues.String,
                        StyleIndex = 1
                    },
                    new Cell()
                    {
                        CellReference = GetCellReference(7 + evaluations.Count, 8 + project.Steps.Length),
                        CellValue = new CellValue("Jegy"),
                        DataType = CellValues.String,
                        StyleIndex = 1
                    }
                )
                {
                    RowIndex = (uint)(7 + evaluations.Count)
                }
            );

            sheetData.Append(
                new Row(
                    new Cell()
                    {
                        CellReference = GetCellReference(8 + evaluations.Count, 6 + project.Steps.Length),
                        CellValue = new CellValue(-100),
                        DataType = CellValues.Number,
                        StyleIndex = 9
                    },
                    new Cell()
                    {
                        CellReference = GetCellReference(8 + evaluations.Count, 7 + project.Steps.Length),
                        CellFormula = new CellFormula("=0"),
                        DataType = CellValues.Number,
                        StyleIndex = 9
                    },
                    new Cell()
                    {
                        CellReference = GetCellReference(8 + evaluations.Count, 8 + project.Steps.Length),
                        CellValue = new CellValue("1"),
                        DataType = CellValues.Number,
                        StyleIndex = 1
                    }
                )
                {
                    RowIndex = (uint)(8 + evaluations.Count)
                }
            );

            sheetData.Append(
                new Row(
                    new Cell()
                    {
                        CellReference = GetCellReference(9 + evaluations.Count, 6 + project.Steps.Length),
                        CellValue = new CellValue(0.4),
                        DataType = CellValues.Number,
                        StyleIndex = 9
                    },
                    new Cell()
                    {
                        CellReference = GetCellReference(9 + evaluations.Count, 7 + project.Steps.Length),
                        CellFormula = new CellFormula($"={GetCellReference(4, 6 + project.Steps.Length, true)} * {GetCellReference(9 + evaluations.Count, 6 + project.Steps.Length)}"),
                        DataType = CellValues.Number,
                        StyleIndex = 9
                    },
                    new Cell()
                    {
                        CellReference = GetCellReference(9 + evaluations.Count, 8 + project.Steps.Length),
                        CellValue = new CellValue("2"),
                        DataType = CellValues.Number,
                        StyleIndex = 1
                    }
                )
                {
                    RowIndex = (uint)(9 + evaluations.Count)
                }
            );

            sheetData.Append(
                new Row(
                    new Cell()
                    {
                        CellReference = GetCellReference(10 + evaluations.Count, 6 + project.Steps.Length),
                        CellValue = new CellValue(0.55),
                        DataType = CellValues.Number,
                        StyleIndex = 9
                    },
                    new Cell()
                    {
                        CellReference = GetCellReference(10 + evaluations.Count, 7 + project.Steps.Length),
                        CellFormula = new CellFormula($"={GetCellReference(4, 6 + project.Steps.Length, true)} * {GetCellReference(10 + evaluations.Count, 6 + project.Steps.Length)}"),
                        DataType = CellValues.Number,
                        StyleIndex = 9
                    },
                    new Cell()
                    {
                        CellReference = GetCellReference(10 + evaluations.Count, 8 + project.Steps.Length),
                        CellValue = new CellValue("3"),
                        DataType = CellValues.Number,
                        StyleIndex = 1
                    }
                )
                {
                    RowIndex = (uint)(10 + evaluations.Count)
                }
            );

            sheetData.Append(
                new Row(
                    new Cell()
                    {
                        CellReference = GetCellReference(11 + evaluations.Count, 6 + project.Steps.Length),
                        CellValue = new CellValue(0.7),
                        DataType = CellValues.Number,
                        StyleIndex = 9
                    },
                    new Cell()
                    {
                        CellReference = GetCellReference(11 + evaluations.Count, 7 + project.Steps.Length),
                        CellFormula = new CellFormula($"={GetCellReference(4, 6 + project.Steps.Length, true)} * {GetCellReference(11 + evaluations.Count, 6 + project.Steps.Length)}"),
                        DataType = CellValues.Number,
                        StyleIndex = 9
                    },
                    new Cell()
                    {
                        CellReference = GetCellReference(11 + evaluations.Count, 8 + project.Steps.Length),
                        CellValue = new CellValue("4"),
                        DataType = CellValues.Number,
                        StyleIndex = 1
                    }
                )
                {
                    RowIndex = (uint)(11 + evaluations.Count)
                }
            );

            sheetData.Append(
                new Row(
                    new Cell()
                    {
                        CellReference = GetCellReference(12 + evaluations.Count, 6 + project.Steps.Length),
                        CellValue = new CellValue(0.85),
                        DataType = CellValues.Number,
                        StyleIndex = 9
                    },
                    new Cell()
                    {
                        CellReference = GetCellReference(12 + evaluations.Count, 7 + project.Steps.Length),
                        CellFormula = new CellFormula($"={GetCellReference(4, 6 + project.Steps.Length, true)} * {GetCellReference(12 + evaluations.Count, 6 + project.Steps.Length)}"),
                        DataType = CellValues.Number,
                        StyleIndex = 9
                    },
                    new Cell()
                    {
                        CellReference = GetCellReference(12 + evaluations.Count, 8 + project.Steps.Length),
                        CellValue = new CellValue("5"),
                        DataType = CellValues.Number,
                        StyleIndex = 1
                    }
                )
                {
                    RowIndex = (uint)(12 + evaluations.Count)
                }
            );
        }

        private void MergeCells(Project project)
        {
            this.worksheetPart.Worksheet.InsertAfter(
                new MergeCells(
                    new MergeCell() { Reference = new StringValue("A1:A3") },
                    new MergeCell() { Reference = new StringValue("B1:B3") },
                    new MergeCell() { Reference = new StringValue("C1:C2") },
                    new MergeCell() { Reference = new StringValue($"D1:{GetCellReference(1, 3 + project.Steps.Length)}") },
                    new MergeCell() { Reference = new StringValue($"{GetCellReference(1, 4 + project.Steps.Length)}:{GetCellReference(2, 4 + project.Steps.Length)}") },
                    new MergeCell() { Reference = new StringValue($"{GetCellReference(1, 5 + project.Steps.Length)}:{GetCellReference(2, 5 + project.Steps.Length)}") },
                    new MergeCell() { Reference = new StringValue($"{GetCellReference(1, 6 + project.Steps.Length)}:{GetCellReference(3, 7 + project.Steps.Length)}") },
                    new MergeCell() { Reference = new StringValue($"{GetCellReference(1, 8 + project.Steps.Length)}:{GetCellReference(3, 8 + project.Steps.Length)}") },
                    new MergeCell() { Reference = new StringValue($"{GetCellReference(1, 9 + project.Steps.Length)}:{GetCellReference(3, 9 + project.Steps.Length)}") }

                ),
                worksheetPart.Worksheet.Elements<SheetData>().First());
        }

        private void SetColumnWidths(Project project)
        {
            if (this.columns == null)
                this.columns = new Columns();

            columns.Append(
                new Column() { Min = 1, Max = 1, Width = 21, CustomWidth = true },
                new Column() { Min = 2, Max = 2, Width = 5, CustomWidth = true },
                new Column() { Min = 3, Max = 5 + (UInt32)project.Steps.Length, Width = 6, CustomWidth = true },
                new Column() { Min = 6 + (UInt32)project.Steps.Length, Max = 7 + (UInt32)project.Steps.Length, Width = 8, CustomWidth = true },
                new Column() { Min = 8 + (UInt32)project.Steps.Length, Max = 8 + (UInt32)project.Steps.Length, Width = 10, CustomWidth = true },
                new Column() { Min = 9 + (UInt32)project.Steps.Length, Max = 9 + (UInt32)project.Steps.Length, Width = 50, CustomWidth = true }
            );
            worksheetPart.Worksheet.InsertAt(columns, 0);
        }

        private void AddConitionalFormat(string range)
        {

            var conditionalFormatting = new ConditionalFormatting
            (
                new ConditionalFormattingRule
                (
                    new Formula()
                    {
                        Text = "C5<C$4"
                    }
                )
                {
                    Type = ConditionalFormatValues.Expression,
                    FormatId = 0,
                    Priority = 1
                },
                new ConditionalFormattingRule
                (
                    new Formula()
                    {
                        Text = "C5>C$4"
                    }
                )
                {
                    Type = ConditionalFormatValues.Expression,
                    FormatId = 1,
                    Priority = 2
                }

            )
            { 
                SequenceOfReferences = new ListValue<StringValue>()
                {
                    InnerText = range
                }
            };
            worksheetPart.Worksheet.Append(conditionalFormatting);            
        }

        private void AddStyleSheet()
        {
            var styleSheet = new Stylesheet(
                new NumberingFormats(
                    new NumberingFormat() { NumberFormatId = 100, FormatCode = "#,##0.00 \"Ft\"" },
                    new NumberingFormat() { NumberFormatId = 101, FormatCode = "yyyy.MM.dd" },
                    new NumberingFormat() { NumberFormatId = 102, FormatCode = "yyyy.MM.dd. HH:mm:ss" },
                    new NumberingFormat() { NumberFormatId = 10, FormatCode = "0\" pt\""},
                    new NumberingFormat() { NumberFormatId = 11, FormatCode = "0%" },
                    new NumberingFormat() { NumberFormatId = 12, FormatCode = "0\" fő\"" },
                    new NumberingFormat() { NumberFormatId = 13, FormatCode = "0.00" }

                ),
                new Fonts(
                    new Font(                                                               // Index 0 - The default font.
                        new FontSize() { Val = 10 },
                        new FontName() { Val = "Calibri" }),
                    new Font(                                                               // Index 1 - The bold font.
                        new Bold(),
                        new FontSize() { Val = 10 },
                        new FontName() { Val = "Calibri" }),
                    new Font(                                                               // Index 2 - The Italic font.
                        new Italic(),
                        new FontSize() { Val = 10 },
                        new FontName() { Val = "Calibri" }),
                    new Font(                                                               // Index 3 - The bold font. 14 pontos betű
                        new Bold(),
                        new FontSize() { Val = 14 },
                        new FontName() { Val = "Calibri" }),
                    new Font(                                                               // Index 4 - Szürke színű betü
                        new FontSize() { Val = 10 },
                        new FontName() { Val = "Calibri" },
                        new Color() { Rgb = "7F7F7F" }),
                    new Font(                                                               // Index 5 - bold windings
                        new FontSize() { Val = 12 },
                        new FontName() { Val = "Wingdings" }),
                    new Font(                                                               // Index 6 - piros félkövér
                        new Bold(),
                        new FontSize() { Val = 10 },
                        new FontName() { Val = "Calibri" },
                        new Color() { Rgb = "FF0000" })
                ),
                new Fills(
                    new Fill(                                                           // Index 0 - The default fill.
                        new PatternFill() { PatternType = PatternValues.None }),
                    new Fill(                                                           // Index 1 - The default fill of gray 125 (required)
                        new PatternFill() { PatternType = PatternValues.Gray125 }),
                    new Fill(                                                           // Index 2 - Sötétebb szürke háttér
                        new PatternFill(
                            new ForegroundColor() { Rgb = new HexBinaryValue() { Value = "FFBFBFBF" } }
                        )
                        { PatternType = PatternValues.Solid }),
                    new Fill(                                                           // Index 3 - Világosabb szürke háttér
                        new PatternFill(
                            new ForegroundColor() { Rgb = new HexBinaryValue() { Value = "FFD9D9D9" } }
                        )
                        { PatternType = PatternValues.Solid })
                ),
                new Borders(
                    new Border(                                                         // Index 0 - The default border.
                        new LeftBorder(),
                        new RightBorder(),
                        new TopBorder(),
                        new BottomBorder(),
                        new DiagonalBorder()),
                    new Border(                                                         // Index 1 - Applies a Left, Right, Top, Bottom border to a cell
                        new LeftBorder(
                            new Color() { Auto = true }
                        )
                        { Style = BorderStyleValues.Thin },
                        new RightBorder(
                            new Color() { Auto = true }
                        )
                        { Style = BorderStyleValues.Thin },
                        new TopBorder(
                            new Color() { Auto = true }
                        )
                        { Style = BorderStyleValues.Thin },
                        new BottomBorder(
                            new Color() { Auto = true }
                        )
                        { Style = BorderStyleValues.Thin },
                        new DiagonalBorder())
                ),
                new CellFormats(
                    //0
                    new CellFormat() { FontId = 0, FillId = 0, BorderId = 0 },                         // Index 0 - The default cell style.  
                    
                    //1 - bold, középre igazított
                    new CellFormat(
                        new Alignment() 
                        { 
                            Horizontal = HorizontalAlignmentValues.Center, 
                            Vertical = VerticalAlignmentValues.Center, 
                            WrapText = true,
                        }
                    )
                    { FontId = 1, FillId = 0, BorderId = 0, ApplyFont = true },

                    //2 - bold függőleges írás
                    new CellFormat(
                        new Alignment()
                        {
                            Horizontal = HorizontalAlignmentValues.Center,
                            Vertical = VerticalAlignmentValues.Center,
                            WrapText = true,
                            TextRotation = 90
                        }
                    )
                    { FontId = 1, FillId = 0, BorderId = 0, ApplyFont = true },

                    //3 - bold, középre igazított, 14 pontos betű
                    new CellFormat(
                        new Alignment()
                        {
                            Horizontal = HorizontalAlignmentValues.Center,
                            Vertical = VerticalAlignmentValues.Center,
                            WrapText = true,
                        }
                    )
                    { FontId = 3, FillId = 0, BorderId = 0, ApplyFont = true, NumberFormatId = 13 },

                    //4 - "sima" függőleges írás
                    new CellFormat(
                        new Alignment()
                        {
                            Horizontal = HorizontalAlignmentValues.Center,
                            Vertical = VerticalAlignmentValues.Bottom,
                            WrapText = true,
                            TextRotation = 90
                        }
                    )
                    { FontId = 0, FillId = 0, BorderId = 0, ApplyFont = true },

                    //5 - Szürke betű függőleges írás
                    new CellFormat(
                        new Alignment()
                        {
                            Horizontal = HorizontalAlignmentValues.Center,
                            Vertical = VerticalAlignmentValues.Bottom,
                            WrapText = true,
                            TextRotation = 90
                        }
                    )
                    { FontId = 4, FillId = 0, BorderId = 0, ApplyFont = true },

                    //6 - "sima" sötét szürke háttér
                    new CellFormat(
                        new Alignment()
                        {
                            Horizontal = HorizontalAlignmentValues.Center,
                            Vertical = VerticalAlignmentValues.Bottom,
                            WrapText = true
                        }
                    )
                    { FontId = 0, FillId = 2, BorderId = 0, ApplyFont = true },

                    //7 - "sima" világos szürke háttér
                    new CellFormat(
                        new Alignment()
                        {
                            Horizontal = HorizontalAlignmentValues.Center,
                            Vertical = VerticalAlignmentValues.Bottom,
                            WrapText = true
                        }
                    )
                    { FontId = 0, FillId = 3, BorderId = 0, ApplyFont = true },

                    //8 - félkövér sötét szürke háttér
                    new CellFormat(
                        new Alignment()
                        {
                            Horizontal = HorizontalAlignmentValues.Center,
                            Vertical = VerticalAlignmentValues.Bottom,
                            WrapText = true
                        }
                    )
                    { FontId = 1, FillId = 2, BorderId = 0, ApplyFont = true },

                    //9 - normál, középre igazított
                    new CellFormat(
                        new Alignment()
                        {
                            Horizontal = HorizontalAlignmentValues.Center,
                            Vertical = VerticalAlignmentValues.Center,
                            WrapText = true,
                        }
                    )
                    { FontId = 0, FillId = 0, BorderId = 0, ApplyFont = true },

                    //10 - pontok sötét szürke háttér
                    new CellFormat(
                        new Alignment()
                        {
                            Horizontal = HorizontalAlignmentValues.Center,
                            Vertical = VerticalAlignmentValues.Bottom,
                            WrapText = true
                        }
                    )
                    { FontId = 0, FillId = 2, BorderId = 0, ApplyFont = true, NumberFormatId = 10 },

                    //11 - pontok világos szürke háttér
                    new CellFormat(
                        new Alignment()
                        {
                            Horizontal = HorizontalAlignmentValues.Center,
                            Vertical = VerticalAlignmentValues.Bottom,
                            WrapText = true
                        }
                    )
                    { FontId = 0, FillId = 3, BorderId = 0, ApplyFont = true, NumberFormatId = 10 },

                    //12 - félkövér sötét szürke háttér - Windings
                    new CellFormat(
                        new Alignment()
                        {
                            Horizontal = HorizontalAlignmentValues.Center,
                            Vertical = VerticalAlignmentValues.Bottom,
                            WrapText = true
                        }
                    )
                    { FontId = 5, FillId = 2, BorderId = 0, ApplyFont = true },

                    //13 - félkövér Windings 
                    new CellFormat(
                        new Alignment()
                        {
                            Horizontal = HorizontalAlignmentValues.Center,
                            Vertical = VerticalAlignmentValues.Bottom,
                            WrapText = true
                        }
                    )
                    { FontId = 5, FillId = 0, BorderId = 0, ApplyFont = true },

                    //14 - normal sötét szürke háttér - százalék
                    new CellFormat(
                        new Alignment()
                        {
                            Horizontal = HorizontalAlignmentValues.Center,
                            Vertical = VerticalAlignmentValues.Bottom,
                            WrapText = true
                        }
                    )
                    { FontId = 0, FillId = 2, BorderId = 0, ApplyFont = true, NumberFormatId = 11 },

                    //15 - normal sötét szürke háttér - világos szürke betűszín
                    new CellFormat(
                        new Alignment()
                        {
                            Horizontal = HorizontalAlignmentValues.Left,
                            Vertical = VerticalAlignmentValues.Bottom,
                            WrapText = true
                        }
                    )
                    { FontId = 4, FillId = 2, BorderId = 0, ApplyFont = true },

                    //16 - Piros betű
                    new CellFormat(
                        new Alignment()
                        {
                            Horizontal = HorizontalAlignmentValues.Center,
                            Vertical = VerticalAlignmentValues.Center,
                            WrapText = true,
                        }
                    )
                    { FontId = 6, FillId = 0, BorderId = 0, ApplyFont = true },

                    //17 - fő
                    new CellFormat(
                        new Alignment()
                        {
                            Horizontal = HorizontalAlignmentValues.Center,
                            Vertical = VerticalAlignmentValues.Center,
                            WrapText = true,
                        }
                    )
                    { FontId = 1, FillId = 0, BorderId = 0, ApplyFont = true, NumberFormatId = 12 }
                )
            );

            var differentialFormats = new DifferentialFormats
            (
                new DifferentialFormat
                (
                    new Font
                    (
                        new Bold(),
                        new FontSize() { Val = 10 },
                        new FontName() { Val = "Calibri" },
                        new Color() { Rgb = "FF0000" }
                    )
                ),
                new DifferentialFormat
                (
                    new Font
                    (
                        new Bold(),
                        new FontSize() { Val = 10 },
                        new FontName() { Val = "Calibri" },
                        new Color() { Rgb = "FFFFFF" }
                    ),
                    new Fill
                    (                                                           
                        new PatternFill
                        (
                            new BackgroundColor() { Rgb = new HexBinaryValue() { Value = "FF92D050" } }
                        )
                        { 
                            PatternType = PatternValues.Solid 
                        }
                    )
                )
            );

            styleSheet.Append(differentialFormats);

            this.SpreadsheetDocument.WorkbookPart.AddNewPart<WorkbookStylesPart>().Stylesheet = styleSheet;
        }

        private string GetCellReference(int row, int column, bool absolute = false)
        {
            string result;
            if (column <= 26)
                result = $"{(absolute ? "$" : "")}{(Char)(column + 64)}{(absolute ? "$" : "")}{row}";
            else
            {
                int div = column / 26;
                int mod = column % 26;
                if (mod == 0)
                    result = (absolute ? "$" : " ") + (Char)(div + 63) + "Z" + (absolute ? "$" : "") + row.ToString();
                else
                    result = (absolute ? "$" : " ") + (Char)(div + 64) + (Char)(mod + 64) + (absolute ? "$" : "") + row.ToString();
            }
            return result.Trim();
        }
    }
}
