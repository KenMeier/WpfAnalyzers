namespace WpfAnalyzers.Test.PropertyChanged.WPF1014DontRaiseChangeForMissingPropertyTests
{
    using System.Threading.Tasks;

    using NUnit.Framework;

    using WpfAnalyzers.PropertyChanged;

    internal class HappyPath : HappyPathVerifier<WPF1014DontRaiseChangeForMissingProperty>
    {
        [TestCase("null")]
        [TestCase("string.Empty")]
        [TestCase(@"""""")]
        [TestCase(@"""Bar""")]
        [TestCase(@"nameof(Bar)")]
        [TestCase(@"nameof(this.Bar)")]
        public async Task OnPropertyChangedWithEventArgs(string propertyName)
        {
            var testCode = @"
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class ViewModel : INotifyPropertyChanged
    {
        private int bar;

        public event PropertyChangedEventHandler PropertyChanged;

        public int Bar
        {
            get { return this.bar; }
            set
            {
                if (value == this.bar) return;
                this.bar = value;
                this.OnPropertyChanged(new PropertyChangedEventArgs(nameof(Bar)));
            }
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            this.PropertyChanged?.Invoke(this, e);
        }
    }";

            testCode = testCode.AssertReplace(@"nameof(Bar)", propertyName);
            await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
        }

        [Test]
        public async Task OnPropertyChangedCallerMemberName()
        {
            var testCode = @"
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class ViewModel : INotifyPropertyChanged
    {
        private int bar;

        public event PropertyChangedEventHandler PropertyChanged;

        public int Bar
        {
            get { return this.bar; }
            set
            {
                if (value == this.bar) return;
                this.bar = value;
                this.OnPropertyChanged();
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }";

            await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
        }

        [Test]
        public async Task OnPropertyChangedCallerMemberNameCopyLocalNullCheckInvoke()
        {
            var testCode = @"
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class ViewModel : INotifyPropertyChanged
    {
        private int bar;

        public event PropertyChangedEventHandler PropertyChanged;

        public int Bar
        {
            get { return this.bar; }
            set
            {
                if (value == this.bar) return;
                this.bar = value;
                this.OnPropertyChanged();
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = this.PropertyChanged;
            if (handler != null)
            {
                handler.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }";

            await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
        }

        [Test]
        public async Task OnPropertyChangedCallerMemberNameCopyLocalNullCheckImplicitInvoke()
        {
            var testCode = @"
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class ViewModel : INotifyPropertyChanged
    {
        private int bar;

        public event PropertyChangedEventHandler PropertyChanged;

        public int Bar
        {
            get { return this.bar; }
            set
            {
                if (value == this.bar) return;
                this.bar = value;
                this.OnPropertyChanged();
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }";

            await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
        }

        [TestCase("null")]
        [TestCase("string.Empty")]
        [TestCase(@"""""")]
        [TestCase(@"""Bar""")]
        [TestCase(@"nameof(Bar)")]
        [TestCase(@"nameof(this.Bar)")]
        public async Task Invokes(string propertyName)
        {
            var testCode = @"
    using System.ComponentModel;

    public class ViewModel : INotifyPropertyChanged
    {
        private int bar;
        public event PropertyChangedEventHandler PropertyChanged;

        public int Bar
        {
            get { return this.bar; }
            set
            {
                if (value == this.bar)
                {
                    return;
                }

                this.bar = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Bar))));
            }
        }
    }";
            testCode = testCode.AssertReplace(@"nameof(this.Bar))", propertyName);
            await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
        }

        [Test]
        public async Task InvokesCached()
        {
            var testCode = @"
    using System.ComponentModel;

    public class ViewModel : INotifyPropertyChanged
    {
        private static readonly PropertyChangedEventArgs BarPropertyChangedArgs = new PropertyChangedEventArgs(nameof(Bar));
        private int bar;
        public event PropertyChangedEventHandler PropertyChanged;

        public int Bar
        {
            get { return this.bar; }
            set
            {
                if (value == this.bar)
                {
                    return;
                }

                this.bar = value;
                this.PropertyChanged?.Invoke(this, BarPropertyChangedArgs);
            }
        }
    }";
            await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
        }

        [Test]
        public async Task OnPropertyChangedInBaseClass()
        {
            var vmCode = @"
namespace RoslynSandBox
{
    using System.ComponentModel;

    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}";

            var testCode = @"
namespace RoslynSandBox
{
    public class ViewModel : ViewModelBase
    {
        private int value;

        public int Value
        {
            get
            {
                return this.value;
            }

            set
            {
                if (value == this.value)
                {
                    return;
                }

                this.value = value;
                this.OnPropertyChanged(nameof(this.Value));
            }
        }
    }
}";
            await this.VerifyHappyPathAsync(vmCode, testCode).ConfigureAwait(false);
        }

        [Test]
        public async Task RaisesForIndexer()
        {
            var testCode = @"
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class Foo : Collection<int>, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected override void SetItem(int index, int item)
        {
            base.SetItem(index, item);
            this.OnPropertyChanged(""Item[]"");
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }";

            await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
        }

        [Test]
        public async Task IgnoresWhenNotInvokingReproIssue122()
        {
            var extCode = @"
using System.ComponentModel;
public static class PropertyChangedEventArgsExt
{
    public static bool HasPropertyChanged(this PropertyChangedEventArgs e, string propertyName)
    {
        return string.Equals(e.PropertyName, propertyName);
    }
}";

            var testCode = @"
using System.ComponentModel;
using System.Runtime.CompilerServices;

public class ViewModel : INotifyPropertyChanged
{
    private int value;

    public ViewModel()
    {
        this.PropertyChanged += OnPropertyChanged;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public int Value
    {
        get
        {
            return this.value;
        }

        set
        {
            if (value == this.value)
            {
                return;
            }

            this.value = value;
            this.OnPropertyChanged();
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.HasPropertyChanged(""SomeProperty""))
        {
            // do something
        }
    }
}";
            await this.VerifyHappyPathAsync(extCode, testCode).ConfigureAwait(false);
        }

        [Test]
        public async Task RaiseForOtherInstance()
        {
            var testCode = @"
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class ViewModel : INotifyPropertyChanged
    {
        private int value;

        public event PropertyChangedEventHandler PropertyChanged;

        public int Value
        {
            get
            {
                return this.value;
            }

            set
            {
                if (value == this.value)
                {
                    return;
                }

                this.value = value;
                this.OnPropertyChanged();
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void RaiseForChild(string propertyName)
        {
            var vm = new ViewModel();
            vm.OnPropertyChanged(propertyName);
        }
    }";
            await this.VerifyHappyPathAsync(testCode).ConfigureAwait(false);
        }

        [Test]
        public async Task RaiseForOtherInstanceOfOtherType()
        {
            var vmCode = @"
namespace RoslynSandBox
{
    using System.ComponentModel;

    public class ViewModel : INotifyPropertyChanged
    {
        private int value;

        public event PropertyChangedEventHandler PropertyChanged;

        public int Value
        {
            get
            {
                return this.value;
            }

            set
            {
                if (value == this.value)
                {
                    return;
                }

                this.value = value;
                this.OnPropertyChanged(nameof(this.Value));
            }
        }

        public void OnPropertyChanged(string propertyName) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}";

            var testCode = @"
namespace RoslynSandBox
{
    internal class Foo
    {
        public void Bar()
        {
            var vm = new ViewModel();
            vm.OnPropertyChanged(""Value"");
        }
    }
}";
            await this.VerifyHappyPathAsync(vmCode, testCode).ConfigureAwait(false);
        }

        [Test]
        public async Task RaiseForOtherInstanceOfOtherTypeWithBaseClass()
        {
            var vmBaseCode = @"
namespace RoslynSandBox
{
    using System.ComponentModel;

    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}";

            var vmCode = @"
namespace RoslynSandBox
{
    public class ViewModel : ViewModelBase
    {
        private int value;

        public int Value
        {
            get
            {
                return this.value;
            }

            set
            {
                if (value == this.value)
                {
                    return;
                }

                this.value = value;
                this.OnPropertyChanged(nameof(this.Value));
            }
        }
    }
}";

            var testCode = @"
namespace RoslynSandBox
{
    internal class Foo
    {
        public void Bar()
        {
            var vm = new ViewModel();
            vm.OnPropertyChanged(""Value"");
        }
    }
}";
            await this.VerifyHappyPathAsync(new[] { vmBaseCode, vmCode, testCode }).ConfigureAwait(false);
        }
    }
}