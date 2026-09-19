namespace Mapbox.LocationModule.AngleSmoothing
{
	public interface IAngleSmoothing
	{

		void Add(double angle);
		double Calculate();

	}
}
