// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows.Forms;
using PInvoke.Parser;

namespace PInvoke.Controls
{
    public partial class CodeDialog
    {
        private class Data
        {
            public string Text;
            public List<Macro> InitialMacroList;
        }

        private bool _changed;

        public string Code
        {
            get { return m_codeTb.Text; }
            set { m_codeTb.Text = value; }
        }

        public List<Macro> InitialMacroList { get; } = new List<Macro>();

        private void m_okBtn_Click(System.Object sender, System.EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Close();
        }

        private void RunWorker()
        {
            _changed = false;
            m_errorsTb.Text = "Parsing ...";
            Data data = new Data();
            data.InitialMacroList = InitialMacroList.ToList();
            data.Text = m_codeTb.Text;
            m_bgWorker.RunWorkerAsync(data);
        }

        private void OnCodeChanged(object sender, EventArgs e)
        {
            if (m_bgWorker.IsBusy)
            {
                _changed = true;
            }
            else
            {
                RunWorker();
            }
        }

        private void OnCompleteBackgroundCompile(System.Object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            m_errorsTb.Text = (string)e.Result;
            if (_changed)
            {
                RunWorker();
            }
        }

        private static void OnDoBackgroundCompile(System.Object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            string result = null;
            try
            {
                Data data = (Data)e.Argument;
                string code = data.Text;
                NativeCodeAnalyzer analyzer = NativeCodeAnalyzerFactory.CreateForMiniParse(OsVersion.WindowsVista, data.InitialMacroList);
                using (var reader = new StringReader(code))
                {
                    NativeCodeAnalyzerResult parseResult = analyzer.Analyze(reader);
                    ErrorProvider ep = parseResult.ErrorProvider;
                    if (ep.Warnings.Count == 0 && ep.Errors.Count == 0)
                    {
                        result = "None ...";
                    }
                    else
                    {
                        result = ep.CreateDisplayString();
                    }
                }
            }
            catch (Exception ex)
            {
                result = ex.Message;
            }

            e.Result = result;
        }

        private void OnCodeKeyDown(System.Object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == Keys.A && e.Modifiers == Keys.Control)
            {
                m_codeTb.SelectAll();
                e.Handled = true;
            }
            else
            {
                e.Handled = false;
            }
        }
    }

}
