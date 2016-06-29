namespace Terrasoft.UI.WindowsControls.Common
{
	using System;
	using System.Collections.ObjectModel;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;
	using System.Windows.Media;

	#region Class: DragAndDropManager

	public class DragAndDropManager
	{

		#region Fields: Private

		private Point _globalMouseDownPosition;

		// TODO Избавиться от _mainDragSource
		private IDragSource _mainDragSource;
		private bool _isCorrectParents = true;
		private IDropTarget _currentDropTarget;

		#endregion

		#region Constructors: Public

		public DragAndDropManager(Panel rootDropArea) {
			RootDropArea = rootDropArea;
		}

		#endregion

		#region Properties: Private

		private Collection<IDragSource> _dragSources;
		private Collection<IDragSource> DragSources {
			get {
				return _dragSources ?? (_dragSources = new Collection<IDragSource>());
			}
		}

		private Collection<IDropTarget> _dropTargets;
		private Collection<IDropTarget> DropTargets {
			get {
				return _dropTargets ?? (_dropTargets = new Collection<IDropTarget>());
			}
		}

		private bool IsMultipleDrag {
			get {
				return _currentDragSources.Count > 1;
			}
		}

		#endregion

		#region Properties: Public

		private Panel _rootDropArea;
		public Panel RootDropArea {
			get {
				return _rootDropArea;
			}
			private set {
				if (_rootDropArea == value) {
					return;
				}
				if (_rootDropArea != null) {
					ClearRootDropArea();
				}
				_rootDropArea = value;
				if (_rootDropArea != null) {
					InitializeRootDropArea();
				}
			}
		}

		private readonly Collection<FrameworkElement> _currentDisplayDragSources = new Collection<FrameworkElement>();
		public Collection<FrameworkElement> CurrentDisplayDragSources {
			get {
				return _currentDisplayDragSources;
			}
		}

		public bool HasCurrentDisplayDragSources {
			get {
				return _currentDisplayDragSources.Count > 0;
			}
		}

		private readonly Collection<IDragSource> _currentDragSources = new Collection<IDragSource>();
		public Collection<IDragSource> CurrentDragSources {
			get {
				return _currentDragSources;
			}
		}

		public bool HasCurrentDragSources {
			get {
				return _currentDragSources.Count > 0;
			}
		}

		private int _cellSize = 4;
		public int CellSize {
			get {
				return _cellSize;
			}
			set {
				_cellSize = value;
			}
		}

		private Point _currentDragSourceMouseDownPosition;
		public Point CurrentDragSourceMouseDownPosition {
			get {
				return _currentDragSourceMouseDownPosition;
			}
			set {
				_currentDragSourceMouseDownPosition = value;
			}
		}

		private bool _useGrigBinding = true;
		public bool UseGrigBinding {
			get {
				return _useGrigBinding;
			}
			set {
				_useGrigBinding = value;
			}
		}

		public bool TryFindAllowedDropTargetParent {
			get;
			set;
		}

		#endregion

		#region Events: Public

		public event DragEventHandler StartDrag;
		private bool OnStartDrag(MouseEventArgs mouseEventArgs) {
			if (StartDrag == null) {
				return true;
			}
			var args = new DragEventArgs(_mainDragSource, _mainDragSource.Position,
				mouseEventArgs);
			StartDrag(this, args);
			return !args.Cancel;
		}

		public event DragEventHandler BeforeDrag;
		private DragEventArgs OnBeforeDrag(IDragSource dragSource, FrameworkElement displayDragSource, Point position,
				MouseEventArgs mouseEventArgs) {
			if (BeforeDrag == null) {
				return null;
			}
			var args = new DragEventArgs(dragSource, displayDragSource, position, mouseEventArgs);
			BeforeDrag(this, args);
			return args;
		}

		public event DragEventHandler Drag;
		private void OnDrag(IDragSource dragSource, FrameworkElement displayDragSource, Point position,
				MouseEventArgs mouseEventArgs) {
			if (Drag == null) {
				return;
			}
			Drag(this, new DragEventArgs(dragSource, displayDragSource, position, mouseEventArgs));
		}

		public event DropEventHandler Drop;
		private void OnDrop(IDragSource dragSource, IDropTarget dropTarget, FrameworkElement displayDragSource,
				Point position, MouseEventArgs mouseEventArgs) {
			if (Drop == null) {
				return;
			}
			Drop(this, new DropEventArgs(dragSource, dropTarget, displayDragSource, position, mouseEventArgs));
		}

		public event EventHandler InvalidDrop;
		private void OnInvalidDrop() {
			if (InvalidDrop == null) {
				return;
			}
			InvalidDrop(this, new EventArgs());
		}

		public event EventHandler StartDrop;
		private void OnStartDrop() {
			if (StartDrop == null) {
				return;
			}
			StartDrop(this, new EventArgs());
		}

		public event EventHandler EndDrop;
		private void OnEndDrop() {
			if (EndDrop == null) {
				return;
			}
			EndDrop(this, new EventArgs());
		}

		#endregion

		#region Methods: Private

		private static IDropTarget MaxVisibility(IDropTarget el1, IDropTarget el2) {
			if (el1.DropVisibilityPriority > el2.DropVisibilityPriority) {
				return el1;
			}
			if (el1.DropVisibilityPriority < el2.DropVisibilityPriority) {
				return el2;
			}
			DependencyObject parent = VisualTreeHelper.GetParent(el2.Instanse);
			while (parent != null) {
				if (el1 == parent) {
					return el2;
				}
				parent = VisualTreeHelper.GetParent(parent);
			}
			return el1;
		}

		private double GetCoordinateWithCellBinding(double approximateCoordinate) {
			return (Math.Round(approximateCoordinate / CellSize)) * CellSize;
		}

		private bool IsChild(FrameworkElement currentDragSource, FrameworkElement childElement) {
			DependencyObject parent = VisualTreeHelper.GetParent(childElement);
			while (parent != null) {
				if (parent == currentDragSource) {
					return true;
				}
				parent = VisualTreeHelper.GetParent(parent);
			}
			return false;
		}

		private bool IsAcceptableRelationship(FrameworkElement element) {
			foreach (var item in _currentDragSources) {
				if ((item.Instanse == element) || IsChild(item.Instanse, element)) {
					return false;
				}
			}
			return true;
		}

		private bool IsAcceptableTarget(IDropTarget dropTarget) {
			if ((dropTarget == _mainDragSource) || (dropTarget.Instanse.Parent == null)) {
				return false;
			}
			if (dropTarget.HasAllowedDropGroupNames) {
				return dropTarget.AllowedDropGroupNames.Contains(_mainDragSource.DragGroupName);
			}
			if (dropTarget.HasUnallowedDropGroupNames) {
				return !dropTarget.UnallowedDropGroupNames.Contains(_mainDragSource.DragGroupName);
			}
			return true;
		}

		private IDropTarget FindParentAllowedTarget(IDropTarget dropTarget) {
			DependencyObject parent = dropTarget.Instanse.Parent;
			while (parent != null) {
				var parentDropTarget = parent as IDropTarget;
				if ((parentDropTarget != null) && DropTargets.Contains(parentDropTarget) &&
						IsAcceptableTarget(parentDropTarget)) {
					return parentDropTarget;
				}
				parent = VisualTreeHelper.GetParent(parent);
			}
			return null;
		}

		private void ClearRootDropArea() {
			_rootDropArea.MouseMove -= OnMouseMove;
			_rootDropArea.MouseLeftButtonUp -= OnMouseLeftButtonUp;
		}

		private void InitializeRootDropArea() {
			_rootDropArea.MouseMove += OnMouseMove;
			_rootDropArea.MouseLeftButtonUp += OnMouseLeftButtonUp;
		}

		private IDragSource GetParentDragSource(DependencyObject dragCaptureZone) {
			var dragSource = dragCaptureZone as IDragSource;
			if (dragSource != null) {
				return dragSource;
			}
			DependencyObject parent = VisualTreeHelper.GetParent(dragCaptureZone);
			while (parent != null) {
				dragSource = parent as IDragSource;
				if (dragSource != null) {
					return dragSource;
				}
				parent = VisualTreeHelper.GetParent(parent);
			}
			throw new Exception("Невозможно найти родительский \"IDragSource\" элемент для зоны " +
				"захвата перетаскивания");
		}

		private IDragSource FindParentSelectedDragSource(IDragSource dragSource) {
			DependencyObject parent = dragSource.Instanse.Parent;
			while (parent != null) {
				var parentDragSource = parent as IDragSource;
				if ((parentDragSource != null) && parentDragSource.IsSelected &&
						DragSources.Contains(parentDragSource)) {
					return parentDragSource;
				}
				parent = VisualTreeHelper.GetParent(parent);
			}
			return null;
		}

		private bool IsChildOfSelectedDragSource(IDragSource dragSource) {
			return FindParentSelectedDragSource(dragSource) != null;
		}

		private void InitializeDragSource() {
			foreach (var item in DragSources) {
				if (item.IsSelected && !IsChildOfSelectedDragSource(item)) {
					_currentDragSources.Add(item);
				}
			}
			if (!_currentDragSources.Contains(_mainDragSource) &&
					!IsChildOfSelectedDragSource(_mainDragSource)) {
				_currentDragSources.Add(_mainDragSource);
			}
			if (IsMultipleDrag) {
				DependencyObject lastParent = null;
				foreach (var item in _currentDragSources) {
					if ((lastParent != null) && (item.Instanse.Parent != lastParent)) {
						_isCorrectParents = false;
						return;
					}
					lastParent = item.Instanse.Parent;
				}
			}
			_isCorrectParents = true;
		}

		private void InitializeDisplayDragSource() {
			foreach (var item in _currentDragSources) {
				FrameworkElement displayDragSource = item.DisplayDragSource();
				_currentDisplayDragSources.Add(displayDragSource);
				_rootDropArea.Children.Add(displayDragSource);
			}
		}

		private void ClearDrag() {
			_mainDragSource = null;
			_currentDragSources.Clear();
			if (HasCurrentDisplayDragSources) {
				foreach (var item in _currentDisplayDragSources) {
					_rootDropArea.Children.Remove(item);
				}
				_currentDisplayDragSources.Clear();
			}
			CursorManager.ClearGlobalCursor();
		}

		private void MoveDisplayDragSources(MouseEventArgs e) {
			Point mousePosition = e.GetPosition(_rootDropArea);
			foreach (var item in _currentDragSources) {

				// TODO Реализовать без вызова DisplayDragSource
				FrameworkElement displayDragSource = item.DisplayDragSource();
				double left;
				double top;
				if (displayDragSource == item) {
					left = mousePosition.X - _currentDragSourceMouseDownPosition.X;
					top = mousePosition.Y - _currentDragSourceMouseDownPosition.Y;
				} else {
					Point itemPosition = item.GetAbsolutePosition(_rootDropArea);
					left = itemPosition.X + mousePosition.X - _globalMouseDownPosition.X;
					top = itemPosition.Y + mousePosition.Y - _globalMouseDownPosition.Y;
				}
				if (UseGrigBinding) {
					left = GetCoordinateWithCellBinding(left);
					top = GetCoordinateWithCellBinding(top);
				}
				var position = new Point(left, top);
				DragEventArgs args = OnBeforeDrag(item, displayDragSource, position, e);
				if (args != null) {
					position = args.Position;
				}
				Canvas.SetLeft(displayDragSource, position.X);
				Canvas.SetTop(displayDragSource, position.Y);
				OnDrag(item, displayDragSource, position, e);
			}
		}

		private bool CanDrop(IDropTarget dropTarget, MouseEventArgs e) {
			if (!dropTarget.CanMultipleDrop && IsMultipleDrag) {
				return false;
			}
			foreach (var item in _currentDragSources) {
				if (!dropTarget.CanDrop(item, e)) {
					return false;
				}
			}
			return true;
		}

		private void UpdateDropTarget(MouseEventArgs e) {
			if (!_isCorrectParents) {
				CursorManager.SetGlobalCursor(CustomCursors.Ban);
				return;
			}
			IDropTarget dropTarget = GetCorrectDropTarget(e);
			if (dropTarget == null) {
				CursorManager.SetGlobalCursor(CustomCursors.Ban);
				if (_currentDropTarget != null) {
					_currentDropTarget.OnLeaveDropZone();
					_currentDropTarget = null;
				}
			} else {
				if (!CanDrop(dropTarget, e)) {
					CursorManager.SetGlobalCursor(CustomCursors.Ban);
					if (_currentDropTarget != null) {
						_currentDropTarget.OnLeaveDropZone();
						_currentDropTarget = null;
					}
					return;
				}
				CursorManager.ClearGlobalCursor();
				if (_currentDropTarget == dropTarget) {
					if (IsMultipleDrag) {
						dropTarget.OnMoveInDropZone(_currentDragSources, e);
					} else {
						dropTarget.OnMoveInDropZone(_mainDragSource, e);
					}
				} else {
					if (_currentDropTarget != null) {
						_currentDropTarget.OnLeaveDropZone();
					}
					if (IsMultipleDrag) {
						dropTarget.OnEnterDropZone(_currentDragSources, e);
					} else {
						dropTarget.OnEnterDropZone(_mainDragSource, e);
					}
				}
			}
			_currentDropTarget = dropTarget;
		}

		private void OnMouseMove(object sender, MouseEventArgs e) {
			if (_mainDragSource == null) {
				return;
			}
			if (!HasCurrentDisplayDragSources) {
				if (!OnStartDrag(e)) {
					ClearDrag();
					return;
				}
				if (!HasCurrentDragSources) {
					InitializeDragSource();
				}
				InitializeDisplayDragSource();
			}
			MoveDisplayDragSources(e);
			UpdateDropTarget(e);
		}

		private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
			if (_mainDragSource == null) {
				return;
			}
			if (!HasCurrentDisplayDragSources) {
				ClearDrag();
				return;
			}
			OnStartDrop();
			IDropTarget dropTarget = GetCorrectDropTarget(e);
			if (_isCorrectParents && (dropTarget != null) && CanDrop(dropTarget, e)) {
				_currentDropTarget = null;
				dropTarget.OnLeaveDropZone();
				foreach (var item in _currentDragSources) {
					FrameworkElement displayDragSource = item.DisplayDragSource();
					if (displayDragSource.Parent == null) {
						OnDrop(item, dropTarget, displayDragSource, e.GetPosition(_rootDropArea), e);
					} else {
						OnDrop(item, dropTarget, displayDragSource, displayDragSource.TransformToVisual(
							dropTarget.Instanse).Transform(new Point(0, 0)), e);
					}
				}
			} else {
				OnInvalidDrop();
			}
			ClearDrag();
			OnEndDrop();
		}

		private void OnDragSourceCaptureZoneMouseDown(object sender, MouseEventArgs e) {
			if (_mainDragSource != null) {
				return;
			}
			_mainDragSource = GetParentDragSource((DependencyObject)sender);
			_mainDragSource = FindParentSelectedDragSource(_mainDragSource) ?? _mainDragSource;
			_globalMouseDownPosition = e.GetPosition(_rootDropArea);
			_currentDragSourceMouseDownPosition = e.GetPosition(_mainDragSource.Instanse);
		}

		private void RegisterDragCaptureZoneEvents(IDragSource dragSource) {
			UIElement dragCaptureZone = dragSource.DragCaptureZone;
			dragCaptureZone.MouseLeftButtonDown += OnDragSourceCaptureZoneMouseDown;
		}

		private void UnregisterDragCaptureZoneEvents(IDragSource dragSource) {
			UIElement dragCaptureZone = dragSource.DragCaptureZone;
			dragCaptureZone.MouseLeftButtonDown -= OnDragSourceCaptureZoneMouseDown;
		}

		#endregion

		#region Methods: Public

		public void ClearCurrentDragSource() {
			ClearDrag();
			_mainDragSource = null;
		}

		public void SetCurrentDragSource(IDragSource dragSource, string dragGroupName) {
			ClearDrag();
			dragSource.DragGroupName = dragGroupName;
			_mainDragSource = dragSource;
			_currentDragSources.Add(dragSource);
			FrameworkElement instanse = dragSource.Instanse;
			_currentDragSourceMouseDownPosition = new Point(instanse.ActualWidth / 2, instanse.ActualHeight / 2);
		}

		public void RegisterDragSource(IDragSource dragSource) {
			if (DragSources.Contains(dragSource)) {
				throw new Exception("Элемент уже был зарегистрирован ранее");
			}
			DragSources.Add(dragSource);
			RegisterDragCaptureZoneEvents(dragSource);
		}

		public void RemoveDragSource(IDragSource dragSource) {
			if (!DragSources.Contains(dragSource)) {
				throw new Exception("Элемент не найден");
			}
			DragSources.Remove(dragSource);
			UnregisterDragCaptureZoneEvents(dragSource);
		}

		public void RegisterDropTarget(IDropTarget dropTarget) {
			if (!DropTargets.Contains(dropTarget)) {
				DropTargets.Add(dropTarget);
			}
		}

		public void RegisterDropTarget(IDropTarget dropTarget, params string[] allowedDropGroupNames) {
			if (!DropTargets.Contains(dropTarget)) {
				DropTargets.Add(dropTarget);
			}
			foreach (var item in allowedDropGroupNames) {
				dropTarget.AllowedDropGroupNames.Add(item);
			}
		}

		public void RemoveDropTarget(IDropTarget dropTarget) {
			if (!DropTargets.Contains(dropTarget)) {
				throw new Exception("Элемент не найден");
			}
			DropTargets.Remove(dropTarget);
		}

		public IDropTarget GetCorrectDropTarget(MouseEventArgs e) {
			IDropTarget lastCorrectDropTarget = null;
			foreach (var item in DropTargets) {
				FrameworkElement instanse = item.Instanse;
				if (instanse.Parent == null) {
					continue;
				}
				if (item.IsInDropZone(_mainDragSource, e) && IsAcceptableRelationship(instanse)) {
					if (lastCorrectDropTarget == null) {
						lastCorrectDropTarget = item;
						continue;
					}
					lastCorrectDropTarget = MaxVisibility(lastCorrectDropTarget, item);
				}
			}
			if (lastCorrectDropTarget != null) {
				if (IsAcceptableTarget(lastCorrectDropTarget)) {
					return lastCorrectDropTarget;
				}
				if (TryFindAllowedDropTargetParent) {
					return FindParentAllowedTarget(lastCorrectDropTarget);
				}
			}
			return null;
		}

		public void ClearDropTargets() {
			DropTargets.Clear();
		}

		public void ClearDragSources() {
			foreach (var item in DragSources) {
				UnregisterDragCaptureZoneEvents(item);
			}
			DragSources.Clear();
		}

		#endregion

	}

	#endregion

}
