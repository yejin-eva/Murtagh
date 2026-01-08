# Murtagh

Custom inspector attributes for Unity.<br>
Attributes that are good to have, but not too extensive, perfect for prototyping or personal projects with low pressure.<br>

## Features

### Decorator Attributes
- **InfoBox** - Display info/warning/error messages above fields
- **HorizontalLine** - Display horizontal lines to separate sections

### Grouping Attributes
- **Foldout** - Group fields into collapsible foldout sections

### Drawer Attributes
- **ReadOnly** - Make fields non-editable in the inspector
- **ResizableTextArea** - Auto-resizing text area for strings

### Conditional Attributes
- **ShowIf / HideIf** - Conditionally show/hide fields based on other values
- **EnableIf / DisableIf** - Conditionally enable/disable fields based on other values

### Validator Attributes
- **MinValue** - Enforce minimum value for numeric fields
- **MaxValue** - Enforce maximum value for numeric fields
- **Required** - Ensure reference fields are assigned
- **ValidateInput** - Custom validation using your own function

### Additional Features
- Works with nested classes and lists
- Supports ReorderableList with drag-and-drop
- Recursive support for lists within lists

### Performance (Just for keeping track)
Optimized with attribute caching for smooth editor performance:

| Metric           | With Cache | Without Cache | Improvement   |
|------------------|------------|---------------|---------------|
| Avg              | 3.15ms     | 7.49ms        | 2.4x faster   |
| Min              | 1.66ms     | 5.37ms        | 3.2x faster   |
| Reflection calls | 79         | 26,800        | 99.7% avoided |

## Support
This project is an open-source project I am developing for fun and learning. If you would like to support me, please consider: 
- ⭐ Starring the repository
- Sharing it with others who might find it useful
- Donate via [PayPal](https://paypal.me/yejineva) (honestly, any amount helps, esp in this economy😂)

## Credits

Inspired by [NaughtyAttributes by dbrizov](https://github.com/dbrizov/NaughtyAttributes) <br>
I noticed this developer made changes a very long time ago and doesnt support attributes for nested lists, so I made some modifications.
  
