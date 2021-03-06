namespace WpfAnalyzers.Test.DependencyProperties.WPF0013ClrMethodMustMatchRegisteredType
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    using WpfAnalyzers.DependencyProperties;

    internal class HappyPath : HappyPathVerifier<WPF0013ClrMethodMustMatchRegisteredType>
    {
        [TestCase("int")]
        [TestCase("int?")]
        [TestCase("int[]")]
        [TestCase("int?[]")]
        [TestCase("Nullable<int>")]
        [TestCase("ObservableCollection<int>")]
        public async Task AttachedProperty(string typeName)
        {
            var testCode = @"
using System;
using System.Collections.ObjectModel;
using System.Windows;

public static class Foo
{
    public static readonly DependencyProperty BarProperty = DependencyProperty.RegisterAttached(
        ""Bar"",
        typeof(int),
        typeof(Foo),
        new PropertyMetadata(default(int)));

    public static void SetBar(FrameworkElement element, int value)
    {
        element.SetValue(BarProperty, value);
    }

    public static int GetBar(FrameworkElement element)
    {
        return (int)element.GetValue(BarProperty);
    }
}";
            testCode = testCode.AssertReplace("int", typeName);
            await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
        }

        [TestCase("int")]
        [TestCase("int?")]
        [TestCase("int[]")]
        [TestCase("int?[]")]
        [TestCase("Nullable<int>")]
        [TestCase("ObservableCollection<int>")]
        public async Task AttachedPropertyExtensionMethods(string typeName)
        {
            var testCode = @"
using System;
using System.Collections.ObjectModel;
using System.Windows;

public static class Foo
{
    public static readonly DependencyProperty BarProperty = DependencyProperty.RegisterAttached(
        ""Bar"",
        typeof(int),
        typeof(Foo),
        new PropertyMetadata(default(int)));

    public static void SetBar(this FrameworkElement element, int value)
    {
        element.SetValue(BarProperty, value);
    }

    public static int GetBar(this FrameworkElement element)
    {
        return (int)element.GetValue(BarProperty);
    }
}";
            testCode = testCode.AssertReplace("int", typeName);
            await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
        }

        [Test]
        public async Task AttachedPropertyWhenBoxed()
        {
            var booleanBoxesCode = @"
internal static class BooleanBoxes
{
    internal static readonly object True = true;
    internal static readonly object False = false;

    internal static object Box(bool value)
    {
        return value
                    ? True
                    : False;
    }
}";

            var testCode = @"
using System;
using System.Windows;

public static class Foo
{
    public static readonly DependencyProperty BarProperty = DependencyProperty.RegisterAttached(
        ""Bar"",
        typeof(bool),
        typeof(Foo),
        new PropertyMetadata(default(bool)));

    public static void SetBar(FrameworkElement element, bool value)
    {
        element.SetValue(BarProperty, BooleanBoxes.Box(value));
    }

    public static bool GetBar(FrameworkElement element)
    {
        return (bool)element.GetValue(BarProperty);
    }
}";
            await this.VerifyHappyPathAsync(new[] { testCode, booleanBoxesCode }).ConfigureAwait(false);
        }

        [Test]
        public async Task AttachedPropertySettingValueInCallback()
        {
            var testCode = @"
using System.Windows;

public static class Foo
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.RegisterAttached(
        ""Value"",
        typeof(int),
        typeof(Foo),
        new PropertyMetadata(
            default(int),
            OnValueChanged));

    public static void SetValue(this DependencyObject element, int value)
    {
        element.SetValue(ValueProperty, value);
    }

    [AttachedPropertyBrowsableForChildren(IncludeDescendants = false)]
    [AttachedPropertyBrowsableForType(typeof(DependencyObject))]
    public static int GetValue(this DependencyObject element)
    {
        return (int)element.GetValue(ValueProperty);
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        d.SetValue(ValueProperty, e.NewValue);
    }
}";
            await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
        }

        [Test]
        public async Task ReadOnlyAttachedProperty()
        {
            var testCode = @"
using System.Windows;

public static class Foo
{
    private static readonly DependencyPropertyKey BarPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
        ""Bar"",
        typeof(int),
        typeof(Foo),
        new PropertyMetadata(default(int)));

        public static readonly DependencyProperty BarProperty = BarPropertyKey.DependencyProperty;

    public static void SetBar(this FrameworkElement element, int value) => element.SetValue(BarPropertyKey, value);

    public static int GetBar(this FrameworkElement element) => (int)element.GetValue(BarProperty);
}";

            await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
        }
    }
}