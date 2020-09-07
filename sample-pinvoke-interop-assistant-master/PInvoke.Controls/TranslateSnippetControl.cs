// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Forms;
using PInvoke.Parser;
using PInvoke.Transform;
using System.IO;

namespace PInvoke.Controls
{
    public partial class TranslateSnippetControl
    {
        private class RequestData
        {
            internal string Text;
            internal ReadOnlyCollection<Macro> InitialMacroList;
        }

        private class ResponseData
        {
            internal string ParseOutput;
        }

        private INativeSymbolStorage _storage;
        private TransformKindFlags _transKind = TransformKindFlags.All;
        private List<Macro> _initialMacroList = new List<Macro>();

        private bool _changed;
        public bool AutoGenerate
        {
            get { return _autoGenerateButton.Checked; }
            set { _autoGenerateButton.Checked = value; }
        }


        public TranslateSnippetControl()
        {
            // This call is required by the Windows Form Designer.
            InitializeComponent();

            _storage = new BasicSymbolStorage();

            // Add any initialization after the InitializeComponent() call.
            _languageTypeComboBox.Items.AddRange(EnumUtil.GetAllValuesObject<LanguageType>());
            _languageTypeComboBox.SelectedItem = LanguageType.VisualBasic;
        }

        public TranslateSnippetControl(INativeSymbolStorage storage)
        {
            _storage = storage;
        }

        #region "ISignatureImportControl"

        /// <summary>
        /// Language that we are displaying the generated values in
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public LanguageType LanguageType
        {
            get
            {
                if (_languageTypeComboBox.SelectedItem == null)
                {
                    return Transform.LanguageType.VisualBasic;
                }

                return (LanguageType)_languageTypeComboBox.SelectedItem;
            }
            set { _languageTypeComboBox.SelectedItem = value; }
        }

        public INativeSymbolStorage Storage
        {
            get { return _storage; }
            set
            {
                _storage = value;
                _initialMacroList = null;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TransformKindFlags TransformKindFlags
        {
            get { return _transKind; }
            set { _transKind = value; }
        }

        public event EventHandler LanguageTypeChanged;

        public string ManagedCode
        {
            get { return _managedCodeTextBox.Text; }
        }

        #endregion

        #region "Event Handlers"

        private void OnNativeCodeChanged(object sender, EventArgs e)
        {
            if (_backgroundWorker.IsBusy)
            {
                _changed = true;
            }
            else
            {
                RunWorker();
            }
        }

        private static void OnDoBackgroundWork(System.Object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            ResponseData result = new ResponseData();
            try
            {
                RequestData req = (RequestData)e.Argument;
                string code = req.Text;
                NativeCodeAnalyzer analyzer = NativeCodeAnalyzerFactory.CreateForMiniParse(OsVersion.WindowsVista, req.InitialMacroList);

                // TODO: shouldn't include this
                analyzer.IncludePathList.Add("c:\\program files (x86)\\windows kits\\8.1\\include\\shared");
                using (var reader = new StringReader(code))
                {
                    NativeCodeAnalyzerResult parseResult = analyzer.Analyze(reader);
                    ErrorProvider ep = parseResult.ErrorProvider;
                    if (ep.Warnings.Count == 0 && ep.Errors.Count == 0)
                    {
                        result.ParseOutput = "None ...";
                    }
                    else
                    {
                        result.ParseOutput = ep.CreateDisplayString();
                    }
                }
            }
            catch (Exception ex)
            {
                result.ParseOutput = ex.Message;
            }

            e.Result = result;
        }

        private void OnBackgroundOperationCompleted(System.Object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            ResponseData response = (ResponseData)e.Result;
            m_errorsTb.Text = response.ParseOutput;
            if (_changed)
            {
                RunWorker();
            }
            else if (_autoGenerateButton.Checked)
            {
                GenerateCode();
            }

        }

        private void OnGenerateCodeClick(object sender, EventArgs e)
        {
            GenerateCode();
        }

        private void OnAutoGenerateCodeCheckChanged(object sender, EventArgs e)
        {
            if (_autoGenerateButton.Checked)
            {
                GenerateCode();
            }
        }

        private void OnLanguageTypeChanged(object sender, EventArgs e)
        {
            if (_autoGenerateButton.Checked)
            {
                GenerateCode();
            }

            if (LanguageTypeChanged != null)
            {
                LanguageTypeChanged(this, EventArgs.Empty);
            }
        }

        private void m_nativeCodeTb_KeyDown(System.Object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == Keys.A & e.Modifiers == Keys.Control)
            {
                _nativeCodeTextBox.SelectAll();
                e.Handled = true;
            }
        }

        #endregion

        #region "Helpers"

        private void RunWorker()
        {
            _changed = false;
            m_errorsTb.Text = "Parsing ...";
            if (_initialMacroList == null)
            {
                _initialMacroList = _storage.GetAllMacros().ToList();
            }

            RequestData data = new RequestData();
            data.InitialMacroList = new ReadOnlyCollection<Macro>(_initialMacroList);
            data.Text = _nativeCodeTextBox.Text;
            _backgroundWorker.RunWorkerAsync(data);
        }

        private void GenerateCode()
        {
            try
            {
                var conv = new BasicConverter(LanguageType, _storage);
                conv.TransformKindFlags = _transKind;
                _managedCodeTextBox.Code = conv.ConvertNativeCodeToPInvokeCode(_nativeCodeTextBox.Text);
            }
            catch (Exception ex)
            {
                _managedCodeTextBox.Code = ex.Message;
            }
        }

        #endregion

    }

}

//=======================================================
//Service provided by Telerik (www.telerik.com)
//Conversion powered by NRefactory.
//Twitter: @telerik
//Facebook: facebook.com/telerik
//=======================================================
