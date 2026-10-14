using System;
using System.Collections.Generic;
using HtmlToUitk.Convert.Dialect;
using HtmlToUitk.Core.Html;
using HtmlToUitk.Core.Uxml;

namespace HtmlToUitk.Convert.Emulations
{
    public sealed class ElementEditor : IElementEditor
    {
        private readonly UiDocument _document;

        public ElementEditor(UiDocument document)
        {
            _document = document;
        }

        public void InsertChild(UxmlElement parent, int index, UxmlElement child)
        {
            var list = new List<UxmlElement>(parent.Children);
            if (index < 0 || index > list.Count) index = list.Count;
            list.Insert(index, child);
            parent.Children = list;
            child.Parent = parent;
        }

        public void AppendChild(UxmlElement parent, UxmlElement child)
        {
            InsertChild(parent, parent.Children.Count, child);
        }

        public void RemoveElement(UxmlElement element)
        {
            if (element.Parent != null)
            {
                var list = new List<UxmlElement>(element.Parent.Children);
                list.Remove(element);
                element.Parent.Children = list;
            }
            else
            {
                var roots = new List<UxmlElement>(_document.Roots);
                roots.Remove(element);
                _document.Roots = roots;
            }

            element.Parent = null;
        }

        public void ReplaceElement(UxmlElement element, UxmlElement replacement)
        {
            if (element.Parent != null)
            {
                var list = new List<UxmlElement>(element.Parent.Children);
                int index = list.IndexOf(element);
                if (index >= 0) list[index] = replacement;
                element.Parent.Children = list;
                replacement.Parent = element.Parent;
            }
            else
            {
                var roots = new List<UxmlElement>(_document.Roots);
                int index = roots.IndexOf(element);
                if (index >= 0) roots[index] = replacement;
                _document.Roots = roots;
                replacement.Parent = null;
            }

            element.Parent = null;
        }

        public UxmlElement Wrap(UxmlElement element, UxmlElement wrapper)
        {
            UxmlElement parent = element.Parent;
            if (parent != null)
            {
                var list = new List<UxmlElement>(parent.Children);
                int index = list.IndexOf(element);
                if (index >= 0) list[index] = wrapper;
                parent.Children = list;
                wrapper.Parent = parent;
            }
            else
            {
                var roots = new List<UxmlElement>(_document.Roots);
                int index = roots.IndexOf(element);
                if (index >= 0) roots[index] = wrapper;
                _document.Roots = roots;
                wrapper.Parent = null;
            }

            var children = new List<UxmlElement>(wrapper.Children) { element };
            wrapper.Children = children;
            element.Parent = wrapper;
            return wrapper;
        }

        public void MoveChildren(UxmlElement from, UxmlElement to)
        {
            var moved = new List<UxmlElement>(to.Children);
            foreach (UxmlElement child in from.Children)
            {
                moved.Add(child);
                child.Parent = to;
            }

            to.Children = moved;
            from.Children = new List<UxmlElement>();
        }

        public void AddClass(UxmlElement element, string className)
        {
            var classes = new List<string>(ClassList.Split(element.GetAttribute("class")));
            if (classes.Contains(className)) return;
            classes.Add(className);
            element.Attributes = AttrOrder.With(element.Attributes, "class", string.Join(" ", classes));
        }

        public void RemoveClass(UxmlElement element, string className)
        {
            var classes = new List<string>(ClassList.Split(element.GetAttribute("class")));
            if (!classes.Remove(className)) return;
            if (classes.Count == 0) element.Attributes = AttrOrder.Without(element.Attributes, "class");
            else element.Attributes = AttrOrder.With(element.Attributes, "class", string.Join(" ", classes));
        }

        public void SetAttribute(UxmlElement element, string name, string value)
        {
            element.Attributes = AttrOrder.With(element.Attributes, name, value);
        }

        public static UxmlElement Create(string type, params string[] nameValuePairs)
        {
            var attrs = new List<KeyValuePair<string, string>>();
            for (int i = 0; i + 1 < nameValuePairs.Length; i += 2) attrs.Add(new KeyValuePair<string, string>(nameValuePairs[i], nameValuePairs[i + 1]));
            return new UxmlElement { UxmlType = type, Attributes = AttrOrder.Sorted(attrs) };
        }
    }
}
