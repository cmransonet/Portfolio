// I certify that this assignment is entirely my own work.
// Chet Ransonet
// cxr4680
// CMPS 415 Fall 2016
// Assignment 1

#include <GLFW/glfw3.h>

#include <stdlib.h>
#include <stdio.h>

#pragma comment (lib, "opengl32.lib")
#pragma comment (lib, "glfw3.lib")

#define WIDTH 400		// width of window (also frame buffer's width)
#define HEIGHT 300		// height of window (also frame buffer's height)

static GLubyte frame_buffer[HEIGHT][WIDTH][3];

// used to check if the first endpoint has been chosen
bool firstPointSet = false;

// coordinates for first endpoint of line being drawn
double x1 = 0.0, y1 = 0.0;

// color start and delta values
double R1 = 0, G1 = 0, B1 = 255;
double dR = 255, dG = 255, dB = -255;

/* Called when an error occurs in GLFW initialization: */
void error_callback(int error, const char* description)
{
	fprintf(stderr, "Error: %s\n", description);
}

void ColorPixel(double x, double y, double R, double G, double B)
{
	frame_buffer[(int)y][(int)x][0] = (GLubyte)R;
	frame_buffer[(int)y][(int)x][1] = (GLubyte)G;
	frame_buffer[(int)y][(int)x][2] = (GLubyte)B;
}

// Add a percentage (mod) of color (R,G,B) to the designated pixel (x,y), while capping the color value at 255
//  'mod' is the percentage of color to be added to the pixel
void AddColor(double x, double y, double R, double G, double B, double mod)
{
	// Red
	if (frame_buffer[(int)y][(int)x][0] * (1 - mod) + (R * mod) > 255) // if the total value of the color would exceed 255
		frame_buffer[(int)y][(int)x][0] = 255;							// cap it at 255
	else
		frame_buffer[(int)y][(int)x][0] = (GLubyte)(frame_buffer[(int)y][(int)x][0] * (1 - mod) + R * mod); // color of pixel = (existing color) * (1-mod) + (new color) * mod

	// Green
	if (frame_buffer[(int)y][(int)x][1] * (1 - mod) + G * mod > 255)
		frame_buffer[(int)y][(int)x][1] = 255;
	else
		frame_buffer[(int)y][(int)x][1] = (GLubyte)(frame_buffer[(int)y][(int)x][1] * (1 - mod) + G * mod);

	// Blue
	if (frame_buffer[(int)y][(int)x][2] * (1 - mod) + B * mod > 255)
		frame_buffer[(int)y][(int)x][2] = 255;
	else
		frame_buffer[(int)y][(int)x][2] = (GLubyte)(frame_buffer[(int)y][(int)x][2] * (1 - mod) + B * mod);
}

//  DrawLine gets the coordinates of the second mouse-selected endpoint.
//   The first endpoint is used as the origin of a coordinate plane with right being positive x and up being positive y.
//   The line changes from blue at the first endpoint to yellow at the second.
void DrawLine(double x2, double y2)
{
	// precomputed values
	double m, w, d = 0.5;
	double dx = x2 - x1, dy = y2 - y1;

	
	m = dy / dx;

	printf("Drawing Line from (%f, %f) to (%f, %f), m = %f\n", x1, y1, x2, y2, m);

	// 'current' coordinates
	double x = x1, y = y1;

	double R = R1, G = G1, B = B1;
	
	if (m == 0) // horizontal line
	{
		if (dx > 0) // going up
		{
			while (x < x2)
			{
				  
				ColorPixel(x, y, R, G, B);
				AddColor(x, y + 1, R*d, G*d, B*d, d);
				AddColor(x, y - 1, R*(1 - d), G*(1 - d), B*(1 - d), 1 - d);
				R += (dR / dx);
				G += (dG / dx);
				B += (dB / dx);

				x++;
			}
		}
		else // going down
		{
			while (x > x2)
			{
				  
				ColorPixel(x, y, R, G, B);
				AddColor(x, y - 1, R*d, G*d, B*d, d);
				AddColor(x, y + 1, R*(1 - d), G*(1 - d), B*(1 - d), 1 - d);
				R -= (dR / dx);
				G -= (dG / dx);
				B -= (dB / dx);

				x--;
			}
		}
	}
	else if (dx == 0) // vertical line
	{
		if (dy > 0)
		{
			while (y < y2)
			{
				  
				ColorPixel(x, y, R, G, B);
				AddColor(x + 1, y, R*d, G*d, B*d, d);
				AddColor(x - 1, y, R*(1 - d), G*(1 - d), B*(1 - d), 1 - d);
				R += (dR / dy);
				G += (dG / dy);
				B += (dB / dy);

				y++;
			}
		}
		else
		{
			while (y > y2)
			{
				  
				ColorPixel(x, y, R, G, B);
				AddColor(x + 1, y, R*d, G*d, B*d, d);
				AddColor(x - 1, y, R*(1 - d), G*(1 - d), B*(1 - d), 1 - d);
				R -= (dR / dy);
				G -= (dG / dy);
				B -= (dB / dy);

				y--;
			}
		}
	}
	else if (m > 0 && m <= 1) // slope between 0 and 1
	{
		w = 1.0 - m;

		if (dx > 0)  // shallow up-right line
		{
			while (x < x2)
			{
				  
				ColorPixel(x, y, R, G, B);
				AddColor(x, y + 1, R*d, G*d, B*d, d);
				AddColor(x, y - 1, R*(1 - d), G*(1 - d), B*(1 - d), 1 - d);
				R += (dR / dx);
				G += (dG / dx);
				B += (dB / dx);

				x++; 
				if (d < w)
				{
					d += m;
				}
				else
				{
					y++;
					d -= w;
				}
			}
		}
		else  // shallow down left line
		{
			while (x > x2)
			{
				  
				ColorPixel(x, y, R, G, B);
				AddColor(x, y - 1, R*d, G*d, B*d, d);
				AddColor(x, y + 1, R*(1 - d), G*(1 - d), B*(1 - d), 1 - d);
				R -= (dR / dx);
				G -= (dG / dx);
				B -= (dB / dx);

				x--;
				if (d < w)
				{
					d += m;
				}
				else
				{
					y--;
					d -= w;
				}
			}
		}
	}
	else if (m > 1) // slope greater than 1
	{
		m = dx / dy;
		w = 1 - m;

		if (dx > 0) // steep up right line
		{
			while (y < y2)
			{
				  
				ColorPixel(x, y, R, G, B);
				AddColor(x - 1, y, R*d, G*d, B*d, d);
				AddColor(x + 1, y, R*(1 - d), G*(1 - d), B*(1 - d), 1 - d);
				R += (dR / dy);
				G += (dG / dy);
				B += (dB / dy);

				y++;
				if (d < m)
				{
					x++;
					d += w;
				}
				else
				{
					d -= m;
				}
			}
		}
		else // steep down left line
		{
			while (y > y2)
			{
				  
				ColorPixel(x, y, R, G, B);
				AddColor(x + 1, y, R*d, G*d, B*d, d);
				AddColor(x - 1, y, R*(1 - d), G*(1 - d), B*(1 - d), 1 - d);
				R -= (dR / dy);
				G -= (dG / dy);
				B -= (dB / dy);

				y--;
				if (d < m)
				{
					x--;
					d += w;
				}
				else
				{
					d -= m;
				}
			}
		}
	}
	else if(m >= -1)  // slope between 0 and -1
	{
		m *= -1;
		w = 1.0 - m;

		if (dx < 0)  // shallow up left line
		{
			while (x > x2)
			{
				  
				ColorPixel(x, y, R, G, B);
				AddColor(x, y + 1, R*d, G*d, B*d, d);
				AddColor(x, y - 1, R*(1 - d), G*(1 - d), B*(1 - d), 1 - d);
				R -= (dR / dx);
				G -= (dG / dx);
				B -= (dB / dx);

				x--;
				if (d < w)
				{
					d += m;
				}
				else
				{
					y++;
					d -= w;
				}
			}
		}
		else  // shallow down right line
		{
			while (x < x2)
			{
				ColorPixel(x, y, R, G, B);
				AddColor(x, y - 1, R*d, G*d, B*d, d);
				AddColor(x, y + 1, R*(1 - d), G*(1 - d), B*(1 - d), 1 - d);
				R += (dR / dx);
				G += (dG / dx);
				B += (dB / dx);

				x++; 
				if (d < w)
				{
					d += m;
				}
				else
				{
					y--;
					d -= w;
				}
			}
		}
	}
	else if (m < -1)  // slope less than -1
	{
		m = -dx / dy;
		w = 1 - m;

		if (dx > 0) // steep down right line
		{
			while (y > y2)
			{
				  
				ColorPixel(x, y, R, G, B);
				AddColor(x - 1, y, R*d, G*d, B*d, d);
				AddColor(x + 1, y, R*(1 - d), G*(1 - d), B*(1 - d), 1 - d);
				R -= (dR / dy);
				G -= (dG / dy);
				B -= (dB / dy);

				y--;
				if (d < m)
				{
					x++;
					d += w;
				}
				else
				{
					d -= m;
				}
			}
		}
		else // steep up left line
		{
			while (y < y2)
			{
				  
				ColorPixel(x, y, R, G, B);
				AddColor(x + 1, y, R*d, G*d, B*d, d);
				AddColor(x - 1, y, R*(1 - d), G*(1 - d), B*(1 - d), 1 - d);
				R += (dR / dy);
				G += (dG / dy);
				B += (dB / dy);

				y++;
				if (d < m)
				{
					x--;
					d += w;
				}
				else
				{
					d -= m;
				}
			}
		}
	}

}

void mouse_button_callback(GLFWwindow* window, int button, int action, int mods)
{
	double x_pos = 0.0;
	double y_pos = 0.0;

	glfwGetCursorPos(window, &x_pos, &y_pos);

	if (button == GLFW_MOUSE_BUTTON_LEFT && action == GLFW_PRESS)
	{
		//printf("Mouse button event, button=%d, state=%s, x=%f, y=%f\n", button, (action == GLFW_PRESS ? "Press" : "Release"), x_pos, y_pos);

		if (!firstPointSet)  //save first endpoint
		{
			x1 = x_pos;
			y1 = HEIGHT - y_pos; printf("Set first endpoint to (%f,%f)\n", x1, y1);

			firstPointSet = true;
		}
		else // draw line
		{
			printf("Set second endpoint to (%f,%f)\n", x_pos, HEIGHT - y_pos);

			printf("Calling DrawLine...\n");

			DrawLine(x_pos, HEIGHT - y_pos);

			firstPointSet = false;
		}
	}
}

void display(void)
{
	glViewport(0, 0, WIDTH, HEIGHT);
	glClear(GL_COLOR_BUFFER_BIT);

	// Set the raster position to the lower-left corner to avoid a problem
	// (with glDrawPixels) when the window is resized to smaller dimensions.
	glRasterPos2i(-1, -1);

	// Write the information stored in "frame_buffer" to the color buffer
	glDrawPixels(WIDTH, HEIGHT, GL_RGB, GL_UNSIGNED_BYTE, frame_buffer);
}

int main(void)
{
	GLFWwindow* window;
	glfwSetErrorCallback(error_callback);
	if (!glfwInit())
		exit(EXIT_FAILURE);

	window = glfwCreateWindow(WIDTH, HEIGHT, "cxr4680 Project 1: Line Tool, blue to yellow.", NULL, NULL);
	if (!window)
	{
		glfwTerminate();
		exit(EXIT_FAILURE);
	}

	glfwSetMouseButtonCallback(window, mouse_button_callback);

	glfwMakeContextCurrent(window);

	while (!glfwWindowShouldClose(window))
	{
		display();

		glfwSwapBuffers(window);
		//* For continuous rendering
		// glfwPollEvents(); 
		//* For rendering only in response to events
		glfwWaitEvents();
	}
	glfwDestroyWindow(window);
	glfwTerminate();
	exit(EXIT_SUCCESS);
}
