/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Web.WebPages;
using LessMarkup.Interfaces.System;

namespace LessMarkup.Engine.Build.Mail
{
    public abstract class TemplateInstance : WebPageExecutingBase, IDisposable
    {
        private readonly StringBuilder _builder = new StringBuilder();
        private readonly StringWriter _writer;

        protected TemplateInstance()
        {
            #region Dirty-dirty hack to disable instrumentation. Another way would be to implement all WebPageExecutingBase logic here.

            var property = GetType().GetProperty("InstrumentationService", BindingFlags.Instance | BindingFlags.NonPublic);

            if (property != null)
            {
                var instrumentationService = property.GetValue(this);
                if (instrumentationService != null)
                {
                    property = instrumentationService.GetType().GetProperty("IsAvailable", BindingFlags.Instance | BindingFlags.Public);
                    if (property != null)
                    {
                        property.SetValue(instrumentationService, false);
                    }
                }
            }

            #endregion

            _writer = new StringWriter(_builder);
        }

        protected string Result { get { return _builder.ToString(); } }

        public override void Execute()
        {
            throw new MethodAccessException();
        }

        public override void Write(HelperResult result)
        {
            WriteTo(_writer, result);
        }

        public override void Write(object value)
        {
            WriteTo(_writer, value);
        }

        public override void WriteLiteral(object value)
        {
            _writer.Write(value);
        }

        protected override TextWriter GetOutputWriter()
        {
            return _writer;
        }

        public void Dispose()
        {
            _writer.Dispose();
        }
    }

    public abstract class TemplateInstance<T> : TemplateInstance, IMailTemplate<T>
    {
        protected T Model { get; set; }

        public string Execute(T model)
        {
            Model = model;
            Execute();
            return Result;
        }
    }
}
