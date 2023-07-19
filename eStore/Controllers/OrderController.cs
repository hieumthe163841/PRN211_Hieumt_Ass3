using BusinessObject;
using ClosedXML.Excel;
using DataAccess.Repository.OrderDetailRepo;
using DataAccess.Repository.OrderRepo;
using DataAccess.Repository.ProductRepo;
using eStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace eStore.Controllers
{
    class OrderExportData
    {
        [Display(Name = "Order ID")]
        public int OrderID { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Order Date")]
        public DateTime OrderDate { get; set; }

        [Display(Name = "Order Total")]
        public decimal OrderTotal { get; set; }
    }

    [Authorize]
    public class OrderController : Controller
    {
        private IOrderRepository orderRepository;
        private IOrderDetailRepository orderDetailRepository;

        public OrderController()
        {
            orderRepository = new OrderRepository();
            orderDetailRepository = new OrderDetailRepository();
        }


        public async Task<IActionResult> Index(DateTime? start, DateTime? end, int? page)
        {
            try
            {
                ViewBag.Start = (start == null) ? "" : start.Value.Date.ToString("yyyy-MM-dd");
                ViewBag.End = (end == null) ? "" : end.Value.Date.ToString("yyyy-MM-dd");

                if (page == null)
                {
                    page = 1;
                }

                int memberId = int.Parse(User.Claims.First(c => c.Type.Equals("MemberId")).Value);
                IEnumerable<Order> orders = orderRepository.GetOrders(memberId);

                if (start != null && end != null)
                {
                    if (start > end)
                    {
                        DateTime? temp = start;
                        start = end;
                        end = temp;
                    }
                    orders = orderRepository.GetOrders(memberId, start.Value, end.Value);
                }
                else if ((start != null && end == null) || (start == null && end != null))
                {
                    throw new Exception("Please fill both of the Start and End Date inputs to filter or leave them blank!");
                }
                else if (start == null && end == null)
                {
                    ViewBag.Start = orders.Min(or => or.OrderDate).Date.ToString("yyyy-MM-dd");
                    ViewBag.End = orders.Max(or => or.OrderDate).Date.ToString("yyyy-MM-dd");
                }

                List<OrderExportData> orderExport = new List<OrderExportData>();

                foreach (var order in orders)
                {
                    decimal total = orderDetailRepository.GetOrderTotal(order.OrderId);
                    ViewData[order.OrderId.ToString()] = Math.Round(total, 2);
                    orderExport.Add(new OrderExportData
                    {
                        OrderID = order.OrderId,
                        Email = order.Member.Email,
                        OrderDate = order.OrderDate,
                        OrderTotal = total
                    });
                }
                HttpContext.Session.SetComplexData("OrderData", orderExport);
                int pageSize = 10;

                return View(await PaginatedList<Order>.CreateAsync(orders.AsQueryable(), page ?? 1, pageSize));
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View();
            }
        }

        public ActionResult Details(int? id)
        {
            try
            {
                if (id == null)
                {
                    throw new Exception("Order ID is not found!!!");
                }
                Order order = orderRepository.GetOrder(id.Value);

                if (order == null)
                {
                    throw new Exception("Product ID is not found!!!");
                }
                order.OrderDetails = orderDetailRepository.GetOrderDetails(order.OrderId).ToList();
                decimal orderTotal = orderDetailRepository.GetOrderTotal(order.OrderId);
                ViewBag.OrderTotal = orderTotal;
                return View(order);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View();
            }
        }

        public IActionResult Delete(int? id)
        {
            try
            {
                if (id == null)
                {
                    throw new Exception("Order ID is not existed!!");
                }
                Order order = orderRepository.GetOrder(id.Value);
                if (order == null)
                {
                    throw new Exception("Order ID is not existed!!");
                }
                order.OrderDetails = orderDetailRepository.GetOrderDetails(order.OrderId).ToList();
                decimal orderTotal = orderDetailRepository.GetOrderTotal(order.OrderId);
                ViewBag.OrderTotal = orderTotal;
                return View(order);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            try
            {
                Order order = orderRepository.GetOrder(id);
                if (order == null)
                {
                    throw new Exception("Order ID is not existed!!");
                }
                orderDetailRepository.DeleteOrderDetails(id);
                orderRepository.DeleteOrder(id);
                TempData["Delete"] = "Delete Order with the ID <strong>" + id + "</strong> successfully!!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View();
            }
        }


    }

    
}
