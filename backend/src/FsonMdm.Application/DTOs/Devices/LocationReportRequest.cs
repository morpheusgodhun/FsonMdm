namespace FsonMdm.Application.DTOs.Devices;

/// <summary>Posted by the agent to report a location fix.</summary>
public record LocationReportRequest(double Latitude, double Longitude, double? Accuracy);
