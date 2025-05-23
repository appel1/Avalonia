using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Diagnostics;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using JetBrains.dotMemoryUnit;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Avalonia.LeakTests
{
    [DotMemoryUnit(FailIfRunWithoutSupport = false)]
    public class ControlTests
    {
        // Need to have the collection as field, so GC will not free it
        private readonly ObservableCollection<string> _observableCollection = new();
        
        public ControlTests(ITestOutputHelper atr)
        {
            DotMemoryUnitTestOutput.SetOutputMethod(atr.WriteLine);
        }

        [Fact]
        public void Canvas_Is_Freed()
        {
            using (Start())
            {
                Func<Window> run = () =>
                {
                    var window = new Window
                    {
                        Content = new Canvas()
                    };

                    window.Show();

                    // Do a layout and make sure that Canvas gets added to visual tree.
                    window.LayoutManager.ExecuteInitialLayoutPass();
                    Assert.IsType<Canvas>(window.Presenter.Child);

                    // Clear the content and ensure the Canvas is removed.
                    window.Content = null;
                    window.LayoutManager.ExecuteLayoutPass();
                    Assert.Null(window.Presenter.Child);

                    return window;
                };

                var result = run();

                // Process all Loaded events to free control reference(s)
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);

                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<Canvas>()).ObjectsCount));
            }
        }

        [Fact]
        public void Named_Canvas_Is_Freed()
        {
            using (Start())
            {
                Func<Window> run = () =>
                {
                    var scope = new NameScope();
                    var window = new Window
                    {
                        Content = new Canvas
                        {
                            Name = "foo"
                        }.RegisterInNameScope(scope)
                    };
                    NameScope.SetNameScope(window, scope);

                    window.Show();

                    // Do a layout and make sure that Canvas gets added to visual tree.
                    window.LayoutManager.ExecuteInitialLayoutPass();
                    Assert.IsType<Canvas>(window.Find<Canvas>("foo"));
                    Assert.IsType<Canvas>(window.Presenter.Child);

                    // Clear the content and ensure the Canvas is removed.
                    window.Content = null;
                    NameScope.SetNameScope(window, null);

                    window.LayoutManager.ExecuteLayoutPass();
                    Assert.Null(window.Presenter.Child);

                    return window;
                };

                var result = run();

                // Process all Loaded events to free control reference(s)
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);

                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<Canvas>()).ObjectsCount));
            }
        }

        [Fact]
        public void ScrollViewer_With_Content_Is_Freed()
        {
            using (Start())
            {
                Func<Window> run = () =>
                {
                    var window = new Window
                    {
                        Content = new ScrollViewer
                        {
                            Content = new Canvas()
                        }
                    };

                    window.Show();

                    // Do a layout and make sure that ScrollViewer gets added to visual tree and its 
                    // template applied.
                    window.LayoutManager.ExecuteInitialLayoutPass();
                    Assert.IsType<ScrollViewer>(window.Presenter.Child);
                    Assert.IsType<Canvas>(((ScrollViewer)window.Presenter.Child).Presenter.Child);

                    // Clear the content and ensure the ScrollViewer is removed.
                    window.Content = null;
                    window.LayoutManager.ExecuteLayoutPass();
                    Assert.Null(window.Presenter.Child);

                    return window;
                };

                var result = run();

                // Process all Loaded events to free control reference(s)
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);

                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<TextBox>()).ObjectsCount));
                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<Canvas>()).ObjectsCount));
            }
        }

        [Fact]
        public void TextBox_Is_Freed()
        {
            using (Start())
            {
                Func<Window> run = () =>
                {
                    var window = new Window
                    {
                        Content = new TextBox()
                    };

                    window.Show();

                    // Do a layout and make sure that TextBox gets added to visual tree and its 
                    // template applied.
                    window.LayoutManager.ExecuteInitialLayoutPass();
                    Assert.IsType<TextBox>(window.Presenter.Child);
                    Assert.NotEmpty(window.Presenter.Child.GetVisualChildren());

                    // Clear the content and ensure the TextBox is removed.
                    window.Content = null;
                    window.LayoutManager.ExecuteLayoutPass();
                    Assert.Null(window.Presenter.Child);

                    return window;
                };

                var result = run();

                // Process all Loaded events to free control reference(s)
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);

                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<TextBox>()).ObjectsCount));
            }
        }

        [Fact]
        public void TextBox_With_Xaml_Binding_Is_Freed()
        {
            using (Start())
            {
                Func<Window> run = () =>
                {
                    var window = new Window
                    {
                        DataContext = new Node { Name = "foo" },
                        Content = new TextBox()
                    };

                    var binding = new Avalonia.Data.Binding
                    {
                        Path = "Name"
                    };

                    var textBox = (TextBox)window.Content;
                    textBox.Bind(TextBox.TextProperty, binding);

                    window.Show();

                    // Do a layout and make sure that TextBox gets added to visual tree and its 
                    // Text property set.
                    window.LayoutManager.ExecuteInitialLayoutPass();
                    Assert.IsType<TextBox>(window.Presenter.Child);
                    Assert.Equal("foo", ((TextBox)window.Presenter.Child).Text);

                    // Clear the content and DataContext and ensure the TextBox is removed.
                    window.Content = null;
                    window.DataContext = null;
                    window.LayoutManager.ExecuteLayoutPass();
                    Assert.Null(window.Presenter.Child);

                    return window;
                };

                var result = run();

                // Process all Loaded events to free control reference(s)
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);

                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<TextBox>()).ObjectsCount));
                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<Node>()).ObjectsCount));
            }
        }

        [Fact]
        public void TextBox_Class_Listeners_Are_Freed()
        {
            using (Start())
            {
                TextBox textBox;

                var window = new Window
                {
                    Content = textBox = new TextBox()
                };

                window.Show();

                // Do a layout and make sure that TextBox gets added to visual tree and its 
                // template applied.
                window.LayoutManager.ExecuteInitialLayoutPass();
                Assert.Same(textBox, window.Presenter.Child);

                // Get the border from the TextBox template.
                var border = textBox.GetTemplateChildren().FirstOrDefault(x => x.Name == "border");

                // The TextBox should have subscriptions to its Classes collection from the
                // default theme.
                Assert.NotEqual(0, textBox.Classes.ListenerCount);

                // Clear the content and ensure the TextBox is removed.
                window.Content = null;
                window.LayoutManager.ExecuteLayoutPass();
                Assert.Null(window.Presenter.Child);

                // Check that the TextBox has no subscriptions to its Classes collection.
                Assert.Null(((INotifyCollectionChangedDebug)textBox.Classes).GetCollectionChangedSubscribers());
            }
        }

        [Fact]
        public void TreeView_Is_Freed()
        {
            using (Start())
            {
                Func<Window> run = () =>
                {
                    var nodes = new[]
                    {
                        new Node
                        {
                            Children = new[] { new Node() },
                        }
                    };

                    TreeView target;

                    var window = new Window
                    {
                        Content = target = new TreeView
                        {
                            DataTemplates =
                            {
                                new FuncTreeDataTemplate<Node>(
                                    (x, _) => new TextBlock { Text = x.Name },
                                    x => x.Children)
                            },
                            ItemsSource = nodes
                        }
                    };

                    window.Show();

                    // Do a layout and make sure that TreeViewItems get realized.
                    window.LayoutManager.ExecuteInitialLayoutPass();
                    Assert.Single(target.GetRealizedContainers());

                    // Clear the content and ensure the TreeView is removed.
                    window.Content = null;
                    window.LayoutManager.ExecuteLayoutPass();
                    Assert.Null(window.Presenter.Child);

                    return window;
                };

                var result = run();

                // Process all Loaded events to free control reference(s)
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);

                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<TreeView>()).ObjectsCount));
            }
        }

        [Fact]
        public void Slider_Is_Freed()
        {
            using (Start())
            {
                Func<Window> run = () =>
                {
                    var window = new Window
                    {
                        Content = new Slider()
                    };

                    window.Show();

                    // Do a layout and make sure that Slider gets added to visual tree.
                    window.LayoutManager.ExecuteInitialLayoutPass();
                    Assert.IsType<Slider>(window.Presenter.Child);

                    // Clear the content and ensure the Slider is removed.
                    window.Content = null;
                    window.LayoutManager.ExecuteLayoutPass();
                    Assert.Null(window.Presenter.Child);

                    return window;
                };

                var result = run();

                // Process all Loaded events to free control reference(s)
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);

                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<Slider>()).ObjectsCount));
            }
        }

        [Fact]
        public void TabItem_Is_Freed()
        {
            using (Start())
            {
                Func<Window> run = () =>
                {
                    var window = new Window
                    {
                        Content = new TabControl
                        {
                            ItemsSource = new[] { new TabItem() }
                        }
                    };

                    window.Show();

                    // Do a layout and make sure that TabControl and TabItem gets added to visual tree.
                    window.LayoutManager.ExecuteInitialLayoutPass();
                    var tabControl = Assert.IsType<TabControl>(window.Presenter.Child);
                    Assert.IsType<TabItem>(tabControl.Presenter.Panel.Children[0]);

                    // Clear the items and ensure the TabItem is removed.
                    tabControl.ItemsSource = null;
                    window.LayoutManager.ExecuteLayoutPass();
                    Assert.Empty(tabControl.Presenter.Panel.Children);

                    return window;
                };

                var result = run();

                // Process all Loaded events to free control reference(s)
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);

                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<TabItem>()).ObjectsCount));
            }
        }

        [Fact]
        public void RendererIsDisposed()
        {
            using (Start())
            {
                var screen1 = new Mock<Screen>(1.75, new PixelRect(new PixelSize(1920, 1080)), new PixelRect(new PixelSize(1920, 966)), true);
                var screens = new Mock<IScreenImpl>();
                screens.Setup(x => x.ScreenFromWindow(It.IsAny<IWindowBaseImpl>())).Returns(screen1.Object);

                var impl = new Mock<IWindowImpl>();
                impl.Setup(r => r.TryGetFeature(It.IsAny<Type>())).Returns(null);
                impl.SetupGet(x => x.RenderScaling).Returns(1);
                impl.SetupProperty(x => x.Closed);
                impl.Setup(x => x.Compositor).Returns(RendererMocks.CreateDummyCompositor());
                impl.Setup(x => x.Dispose()).Callback(() => impl.Object.Closed());
                impl.Setup(x => x.TryGetFeature(It.Is<Type>(t => t == typeof(IScreenImpl)))).Returns(screens.Object);

                AvaloniaLocator.CurrentMutable.Bind<IWindowingPlatform>()
                    .ToConstant(new MockWindowingPlatform(() => impl.Object));
                var window = new Window()
                {
                    Content = new Button()
                };
                window.Show();
                window.Close();
                Assert.True(((CompositingRenderer)window.Renderer).IsDisposed);
            }
        }

        [Fact]
        public void Control_With_Style_RenderTransform_Is_Freed()
        {
            // # Issue #3545
            using (Start())
            {
                Func<Window> run = () =>
                {
                    var window = new Window
                    {
                        Styles =
                        {
                            new Style(x => x.OfType<Canvas>())
                            {
                                Setters =
                                {
                                    new Setter
                                    {
                                        Property = Visual.RenderTransformProperty,
                                        Value = new RotateTransform(45),
                                    }
                                }
                            }
                        },
                        Content = new Canvas()
                    };

                    window.Show();

                    // Do a layout and make sure that Canvas gets added to visual tree with
                    // its render transform.
                    window.LayoutManager.ExecuteInitialLayoutPass();
                    var canvas = Assert.IsType<Canvas>(window.Presenter.Child);
                    Assert.IsType<RotateTransform>(canvas.RenderTransform);

                    // Clear the content and ensure the Canvas is removed.
                    window.Content = null;
                    window.LayoutManager.ExecuteLayoutPass();
                    Assert.Null(window.Presenter.Child);

                    return window;
                };

                var result = run();

                // Process all Loaded events to free control reference(s)
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);

                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<Canvas>()).ObjectsCount));
            }
        }

        [Fact]
        public void Attached_ContextMenu_Is_Freed()
        {
            using (Start())
            {
                void AttachShowAndDetachContextMenu(Control control)
                {
                    var contextMenu = new ContextMenu
                    {
                        Items =
                        {
                            new MenuItem { Header = "Foo" },
                            new MenuItem { Header = "Foo" },
                        }
                    };

                    control.ContextMenu = contextMenu;
                    contextMenu.Open(control);
                    contextMenu.Close();
                    control.ContextMenu = null;
                }

                var window = new Window { Focusable = true };
                window.Show();

                Assert.Same(window, window.FocusManager.GetFocusedElement());

                // Context menu in resources means the baseline may not be 0.
                var initialMenuCount = 0;
                var initialMenuItemCount = 0;
                dotMemory.Check(memory =>
                {
                    initialMenuCount = memory.GetObjects(where => where.Type.Is<ContextMenu>()).ObjectsCount;
                    initialMenuItemCount = memory.GetObjects(where => where.Type.Is<MenuItem>()).ObjectsCount;
                });

                AttachShowAndDetachContextMenu(window);

                // Process all Loaded events to free control reference(s)
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);

                Mock.Get(window.PlatformImpl).Invocations.Clear();
                dotMemory.Check(memory =>
                    Assert.Equal(initialMenuCount, memory.GetObjects(where => where.Type.Is<ContextMenu>()).ObjectsCount));
                dotMemory.Check(memory =>
                    Assert.Equal(initialMenuItemCount, memory.GetObjects(where => where.Type.Is<MenuItem>()).ObjectsCount));
            }
        }
        
        [Fact]
        public void Attached_Control_From_ContextMenu_Is_Freed()
        {
            using (Start())
            {
                var contextMenu = new ContextMenu();
                Func<Window> run = () =>
                {
                    var window = new Window
                    {
                        Content = new TextBlock
                        {
                            ContextMenu = contextMenu
                        }
                    };

                    window.Show();

                    // Do a layout and make sure that TextBlock gets added to visual tree.
                    window.LayoutManager.ExecuteInitialLayoutPass();
                    Assert.IsType<TextBlock>(window.Presenter.Child);

                    // Clear the content and ensure the TextBlock is removed.
                    window.Content = null;
                    window.LayoutManager.ExecuteLayoutPass();
                    Assert.Null(window.Presenter.Child);

                    return window;
                };

                var result = run();

                // Process all Loaded events to free control reference(s)
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);

                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<TextBlock>()).ObjectsCount));
            }
        }

        [Fact]
        public void Standalone_ContextMenu_Is_Freed()
        {
            using (Start())
            {
                void BuildAndShowContextMenu(Control control)
                {
                    var contextMenu = new ContextMenu
                    {
                        Items =
                        {
                            new MenuItem { Header = "Foo" },
                            new MenuItem { Header = "Foo" },
                        }
                    };

                    contextMenu.Open(control);
                    contextMenu.Close();
                }

                var window = new Window { Focusable = true };
                window.Show();

                Assert.Same(window, window.FocusManager.GetFocusedElement());

                // Context menu in resources means the baseline may not be 0.
                var initialMenuCount = 0;
                var initialMenuItemCount = 0;
                dotMemory.Check(memory =>
                {
                    initialMenuCount = memory.GetObjects(where => where.Type.Is<ContextMenu>()).ObjectsCount;
                    initialMenuItemCount = memory.GetObjects(where => where.Type.Is<MenuItem>()).ObjectsCount;
                });

                BuildAndShowContextMenu(window);
                BuildAndShowContextMenu(window);

                // Process all Loaded events to free control reference(s)
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);

                Mock.Get(window.PlatformImpl).Invocations.Clear();
                dotMemory.Check(memory =>
                    Assert.Equal(initialMenuCount, memory.GetObjects(where => where.Type.Is<ContextMenu>()).ObjectsCount));
                dotMemory.Check(memory =>
                    Assert.Equal(initialMenuItemCount, memory.GetObjects(where => where.Type.Is<MenuItem>()).ObjectsCount));
            }
        }

        [Fact]
        public void Path_Is_Freed()
        {
            using (Start())
            {
                var geometry = new EllipseGeometry { Rect = new Rect(0, 0, 10, 10) };

                Func<Window> run = () =>
                {
                    var window = new Window
                    {
                        Content = new Path
                        {
                            Data = geometry
                        }
                    };

                    window.Show();

                    window.LayoutManager.ExecuteInitialLayoutPass();
                    Assert.IsType<Path>(window.Presenter.Child);

                    window.Content = null;
                    window.LayoutManager.ExecuteLayoutPass();
                    Assert.Null(window.Presenter.Child);

                    return window;
                };

                var result = run();

                // Process all Loaded events to free control reference(s)
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);

                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<Path>()).ObjectsCount));

                // We are keeping geometry alive to simulate a resource that outlives the control.
                GC.KeepAlive(geometry);
            }
        }
        
        [Fact]
        public void Polyline_WithObservableCollectionPointsBinding_Is_Freed()
        {
            using (Start())
            {
                var observableCollection = new ObservableCollection<Point>(){new()};

                Func<Window> run = () =>
                {
                    var window = new Window
                    {
                        Content = new Polyline()
                        {
                            Points = observableCollection
                        }
                    };

                    window.Show();

                    window.LayoutManager.ExecuteInitialLayoutPass();
                    Assert.IsType<Polyline>(window.Presenter.Child);

                    window.Content = null;
                    window.LayoutManager.ExecuteLayoutPass();
                    Assert.Null(window.Presenter.Child);

                    return window;
                };

                var result = run();

                // Process all Loaded events to free control reference(s)
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);

                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<Polyline>()).ObjectsCount));

                // We are keeping collection alive to simulate a resource that outlives the control.
                GC.KeepAlive(observableCollection);
            }
        }

        [Fact]
        public void ElementName_Binding_In_DataTemplate_Is_Freed()
        {
            using (Start())
            {
                // Height of ListBoxItem content + padding.
                const double ListBoxItemHeight = 12;
                const double TextBoxHeight = 25;

                var items = new ObservableCollection<int>(Enumerable.Range(0, 10));
                NameScope ns;
                TextBox tb;
                ListBox lb;
                var window = new Window
                {
                    [NameScope.NameScopeProperty] = ns = new NameScope(),
                    Width = 100,
                    Height = (items.Count * ListBoxItemHeight) + TextBoxHeight,
                    Content = new DockPanel
                    {
                        Children =
                        {
                            (tb = new TextBox
                            {
                                Name = "tb",
                                Text = "foo",
                                Height = TextBoxHeight,
                                [DockPanel.DockProperty] = Dock.Top,
                            }),
                            (lb = new ListBox
                            {
                                ItemsSource = items,
                                ItemTemplate = new FuncDataTemplate<int>((_, _) =>
                                    new Canvas
                                    {
                                        Width = 10,
                                        Height = 10,
                                        [!Control.TagProperty] = new Binding
                                        {
                                            ElementName = "tb",
                                            Path = "Text",
                                            NameScope = new WeakReference<INameScope>(ns),
                                        }
                                    }),
                                Padding = new Thickness(0),
                            }),
                        }
                    }
                };

                tb.RegisterInNameScope(ns);

                window.Show();
                window.LayoutManager.ExecuteInitialLayoutPass();

                void AssertInitialItemState()
                {
                    var item0 = (ListBoxItem)lb.GetRealizedContainers().First();
                    var canvas0 = (Canvas)item0.Presenter.Child;
                    Assert.Equal("foo", canvas0.Tag);
                }

                Assert.Equal(10, lb.GetRealizedContainers().Count());
                AssertInitialItemState();

                items.Clear();
                window.LayoutManager.ExecuteLayoutPass();

                Assert.Empty(lb.GetRealizedContainers());

                // Process all Loaded events to free control reference(s)
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);

                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<Canvas>()).ObjectsCount));
            }
        }

        [Fact]
        public void HotKeyManager_Should_Release_Reference_When_Control_Detached()
        {
            using (Start())
            {
                Func<Window> run = () =>
                {
                    var gesture1 = new KeyGesture(Key.A, KeyModifiers.Control);
                    var tl = new Window
                    {
                        Content = new ItemsControl(),
                    };

                    tl.Show();

                    var button = new Button();
                    tl.Content = button;
                    tl.Template = CreateWindowTemplate();
                    tl.ApplyTemplate();
                    tl.Presenter.ApplyTemplate();
                    HotKeyManager.SetHotKey(button, gesture1);

                    // Detach the button from the logical tree, so there is no reference to it
                    tl.Content = null;
                    tl.ApplyTemplate();

                    return tl;
                };

                var result = run();

                // Process all Loaded events to free control reference(s)
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);

                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<Button>()).ObjectsCount));
            }
        }

        [Fact]
        public void HotKeyManager_Should_Release_Reference_When_Control_In_Item_Template_Detached()
        {
            using (Start())
            {
                Func<Window> run = () =>
                {
                    var gesture1 = new KeyGesture(Key.A, KeyModifiers.Control);

                    var tl = new Window { SizeToContent = SizeToContent.WidthAndHeight, IsVisible = true };
                    var lm = tl.LayoutManager;
                    tl.Show();

                    var keyGestures = new AvaloniaList<KeyGesture> { gesture1 };
                    var listBox = new ListBox
                    {
                        Width = 100,
                        Height = 100,
                        // Create a button with binding to the KeyGesture in the template and add it to references list
                        ItemTemplate = new FuncDataTemplate(typeof(KeyGesture), (o, scope) =>
                        {
                            var keyGesture = o as KeyGesture;
                            return new Button
                            {
                                DataContext = keyGesture,
                                [!Button.HotKeyProperty] = new Binding("")
                            };
                        })
                    };
                    // Add the listbox and render it
                    tl.Content = listBox;
                    lm.ExecuteInitialLayoutPass();
                    listBox.ItemsSource = keyGestures;
                    lm.ExecuteLayoutPass();

                    // Let the button detach when clearing the source items
                    keyGestures.Clear();
                    lm.ExecuteLayoutPass();

                    // Add it again to double check,and render
                    keyGestures.Add(gesture1);
                    lm.ExecuteLayoutPass();

                    keyGestures.Clear();
                    lm.ExecuteLayoutPass();

                    return tl;
                };

                var result = run();

                // Process all Loaded events to free control reference(s)
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);

                dotMemory.Check(memory =>
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<Button>()).ObjectsCount));
            }
        }

        [Fact]
        public void ToolTip_Is_Freed()
        {
            using (Start())
            {
                Func<Window> run = () =>
                {
                    var window = new Window();
                    var source = new Button
                    {
                        Template = new FuncControlTemplate<Button>((parent, _) =>
                            new Decorator
                            {
                                [ToolTip.TipProperty] = new TextBlock
                                {
                                    [~TextBlock.TextProperty] = new TemplateBinding(ContentControl.ContentProperty)
                                }
                            }),
                    };

                    window.Content = source;
                    window.Show();

                    var templateChild = (Decorator)source.GetVisualChildren().Single();
                    ToolTip.SetIsOpen(templateChild, true);

                    ToolTip.SetIsOpen(templateChild, false);

                    // Detach the button from the logical tree, so there is no reference to it
                    window.Content = null;
                    
                    // Mock keep reference on a Popup via InvocationsCollection. So let's clear it before. 
                    Mock.Get(window.PlatformImpl).Invocations.Clear();
                    
                    return window;
                };

                var result = run();

                // Process all Loaded events to free control reference(s)
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);

                dotMemory.Check(memory =>
                {
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<TextBlock>()).ObjectsCount);
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<ToolTip>()).ObjectsCount);
                });
            }
        }
        
        [Fact]
        public void Flyout_Is_Freed()
        {
            using (Start())
            {
                Func<Window> run = () =>
                {
                    var window = new Window();
                    var source = new Button
                    {
                        Template = new FuncControlTemplate<Button>((parent, _) =>
                            new Button
                            {
                                Flyout = new Flyout
                                {
                                    Content = new TextBlock
                                    {
                                        [~TextBlock.TextProperty] = new TemplateBinding(ContentControl.ContentProperty)
                                    }
                                }
                            }),
                    };

                    window.Content = source;
                    window.Show();

                    var templateChild = (Button)source.GetVisualChildren().Single();
                    templateChild.Flyout!.ShowAt(templateChild);
                    
                    templateChild.Flyout!.Hide();

                    // Detach the button from the logical tree, so there is no reference to it
                    window.Content = null;

                    // Mock keep reference on a Popup via InvocationsCollection. So let's clear it before. 
                    Mock.Get(window.PlatformImpl).Invocations.Clear();
                    
                    return window;
                };

                var result = run();

                // Process all Loaded events to free control reference(s)
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);

                dotMemory.Check(memory =>
                {
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<TextBlock>()).ObjectsCount);
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<Flyout>()).ObjectsCount);
                    Assert.Equal(0, memory.GetObjects(where => where.Type.Is<Popup>()).ObjectsCount);
                });
            }
        }
        
        private FuncControlTemplate CreateWindowTemplate()
        {
            return new FuncControlTemplate<Window>((parent, scope) =>
            {
                return new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    [~ContentPresenter.ContentProperty] = parent[~ContentControl.ContentProperty],
                }.RegisterInNameScope(scope);
            });
        }

        private IDisposable Start()
        {
            static void Cleanup()
            {
                // KeyboardDevice holds a reference to the focused item.
                KeyboardDevice.Instance.SetFocusedElement(null, NavigationMethod.Unspecified, KeyModifiers.None);
                
                // Empty the dispatcher queue.
                Dispatcher.UIThread.RunJobs();
            }

            return new CompositeDisposable
            {
                Disposable.Create(Cleanup),
                UnitTestApplication.Start(TestServices.StyledWindow.With(
                    focusManager: new FocusManager(),
                    keyboardDevice: () => new KeyboardDevice(),
                    inputManager: new InputManager()))
            };
        }


        private class Node
        {
            public string Name { get; set; }
            public IEnumerable<Node> Children { get; set; }
        }

    }
}
