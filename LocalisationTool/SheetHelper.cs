using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.Office.Interop.Excel;

namespace LocalisationTool
{
    /// <summary>
    /// Helper library for working with Excel Spreadsheets.
    /// </summary>
    class SheetHelper
    {
        public delegate void SheetHandler(Worksheet sheet);
        public delegate void SheetHandlerData(Worksheet sheet, object data);

        public static void Load(String path, bool readOnly, SheetHandler loader)
        {
            Microsoft.Office.Interop.Excel.Application excel = null;
            Microsoft.Office.Interop.Excel.Workbook book = null;
            try
            {
                excel = new Microsoft.Office.Interop.Excel.Application();
                excel.Visible = true;
                book = excel.Workbooks.Open(path, Type.Missing, readOnly);

                Microsoft.Office.Interop.Excel.Worksheet sheet = (Worksheet)book.Worksheets[1];

                loader(sheet);
            }
            catch (System.Exception)
            {

            }
            finally
            {
                if (book != null)
                {
                    book.Close();
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(book);
                }
                if (excel != null)
                {
                    excel.Quit();
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(excel);
                }
            }
        }

        public static void WriteSheet(String path, SheetHandler writer)
        {
            Microsoft.Office.Interop.Excel.Application excel = null;
            Microsoft.Office.Interop.Excel.Workbook book = null;
            try
            {
                excel = new Microsoft.Office.Interop.Excel.Application();
                excel.Visible = true;
                book = excel.Workbooks.Add();

                Microsoft.Office.Interop.Excel.Worksheet sheet = (Worksheet)book.Worksheets[1];

                writer(sheet);

                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                book.SaveAs(path);
            }
            catch (System.Exception ex)
            {

            }
            finally
            {
                if (book != null)
                {
                    book.Close();
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(book);
                }
                if (excel != null)
                {
                    excel.Quit();
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(excel);
                }
            }
        }

        public static void UpdateSheet(String path, SheetHandlerData modifier, object data)
        {
            Microsoft.Office.Interop.Excel.Application excel = null;
            Microsoft.Office.Interop.Excel.Workbook book = null;
            try
            {
                excel = new Microsoft.Office.Interop.Excel.Application();
                excel.Visible = true;
                book = excel.Workbooks.Open(path, Type.Missing, false);

                Microsoft.Office.Interop.Excel.Worksheet sheet = (Worksheet)book.Worksheets[1];

                modifier(sheet, data);

                if (File.Exists(path))
                {
                    book.Save();
                }
            }
            catch (System.Exception ex)
            {

            }
            finally
            {
                if (book != null)
                {
                    book.Close();
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(book);
                }
                if (excel != null)
                {
                    excel.Quit();
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(excel);
                }
            }
        }

        public static int ReadRowToDictionary(Worksheet sheet, int row,
            Dictionary<String, String> target,
            String[] labels)
        {
            int column = 1;
            while (column <= labels.Length)
            {
                Range r = (Range)(sheet.Cells[row, column]);
                if (r != null)
                {
                    if (r.Value != null)
                    {
                        String value = r.Value.ToString();
                        if (!String.IsNullOrEmpty(value))
                        {
                            target[labels[column - 1]] = value;
                        }
                    }
                }
                ++column;
            }
            return column - 1;
        }
    }
}
