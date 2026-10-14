using System;
using System.Collections.Generic;
using HtmlToUitk.Convert;
using HtmlToUitk.Core.Html;

namespace HtmlToUitk.Core.Uxml
{
    public sealed class UxmlElementEditor : IElementEditor
    {
        private readonly UiDocument _document;

        public UxmlElementEditor(UiDocument document = null)
        {
            _document = document;
        }

        private List<UxmlElement> SiblingList(UxmlElement element)
        {
            if (element.Parent != null) return new List<UxmlElement>(element.Parent.Children);
            if (_document != null) return new List<UxmlElement>(_document.Roots);
            throw new InvalidOperationException("the element has no parent and no document was given to the editor");
        }

        private void StoreSiblings(UxmlElement element, UxmlElement parent, List<UxmlElement> list)
        {
            if (parent != null) parent.Children = list;
            else if (_document != null) _document.Roots = list;
            else throw new InvalidOperationException("the element has no parent and no document was given to the editor");
        }

        public void InsertChild(UxmlElement parent, int index, UxmlElement child)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            var list = new List<UxmlElement>(parent.Children);
            if (index < 0 || index > list.Count) throw new ArgumentOutOfRangeException(nameof(index));
            list.Insert(index, child);
            child.Parent = parent;
            parent.Children = list;
        }

        public void AppendChild(UxmlElement parent, UxmlElement child)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            InsertChild(parent, parent.Children.Count, child);
        }

        public void RemoveElement(UxmlElement element)
        {
            List<UxmlElement> list = SiblingList(element);
            int index = list.IndexOf(element);
            if (index < 0) throw new InvalidOperationException("the element is not a child of its parent");
            list.RemoveAt(index);
            StoreSiblings(element, element.Parent, list);
            element.Parent = null;
        }

        public void ReplaceElement(UxmlElement element, UxmlElement replacement)
        {
            List<UxmlElement> list = SiblingList(element);
            int index = list.IndexOf(element);
            if (index < 0) throw new InvalidOperationException("the element is not a child of its parent");
            UxmlElement parent = element.Parent;
            list[index] = replacement;
            replacement.Parent = parent;
            StoreSiblings(element, parent, list);
            element.Parent = null;
        }

        public UxmlElement Wrap(UxmlElement element, UxmlElement wrapper)
        {
            UxmlElement parent = element.Parent;
            List<UxmlElement> list = SiblingList(element);
            int index = list.IndexOf(element);
            if (index < 0) throw new InvalidOperationException("the element is not a child of its parent");
            list[index] = wrapper;
            wrapper.Parent = parent;
            wrapper.Children = new List<UxmlElement>(wrapper.Children) { element };
            element.Parent = wrapper;
            StoreSiblings(element, parent, list);
            return wrapper;
        }

        public void MoveChildren(UxmlElement from, UxmlElement to)
        {
            var list = new List<UxmlElement>(to.Children);
            foreach (UxmlElement child in from.Children)
            {
                child.Parent = to;
                list.Add(child);
            }
            to.Children = list;
            from.Children = new List<UxmlElement>();
        }

        public void AddClass(UxmlElement element, string className)
        {
            var classes = new List<string>(ClassList.Split(element.GetAttribute("class")));
            if (classes.Contains(className)) return;
            classes.Add(className);
            SetAttribute(element, "class", string.Join(" ", classes));
        }

        public void RemoveClass(UxmlElement element, string className)
        {
            var classes = new List<string>(ClassList.Split(element.GetAttribute("class")));
            if (!classes.Remove(className)) return;
            SetAttribute(element, "class", classes.Count == 0 ? null : string.Join(" ", classes));
        }

        public void SetAttribute(UxmlElement element, string name, string value)
        {
            var list = new List<UxmlAttribute>(element.Attributes);
            int index = list.FindIndex(a => string.Equals(a.Name, name, StringComparison.Ordinal));
            if (value == null)
            {
                if (index >= 0) list.RemoveAt(index);
            }
            else if (index >= 0) list[index] = new UxmlAttribute { Name = name, Value = value };
            else list.Add(new UxmlAttribute { Name = name, Value = value });
            element.Attributes = list;
        }
    }
}
