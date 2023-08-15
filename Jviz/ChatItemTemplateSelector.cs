using System.Windows;
using Telerik.Windows.Controls.ConversationalUI;

namespace Jviz
{
    public class ChatItemTemplateSelector : MessageTemplateSelector
    {
        public DataTemplate TextMessageTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is TextMessage)
            {
                return TextMessageTemplate;
            }
            // Handle other message types if needed

            return base.SelectTemplate(item, container);
        }
    }
}