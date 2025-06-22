using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models.Products;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using shortid;
using shortid.Configuration;

namespace Core.Application.Mediatr.Products.Commands
{
    public class ProductCreateCommand : IRequest<string>
    {
        [Required]
        public string StoreUid { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public double Price { get; set; }

        public string Description { get; set; }
        public int Quantity { get; set; } = 1;
        public List<string> CategoryUids { get; set; } = new List<string>();
        public ICollection<ProductMoreInfoRequest> MoreInfos { get; set; } = new List<ProductMoreInfoRequest>();
        public ICollection<ProductAttributeCreateRequest> ProductVariations { get; set; } = new List<ProductAttributeCreateRequest>();
        public ICollection<string> ProductPairArticleCodes { get; set; } = new List<string>();
        public ICollection<string> ProductSimilarArticleCodes { get; set; } = new List<string>();
    }

    public class ProductCreateCommandHandler : IRequestHandler<ProductCreateCommand, string>
    {
        private readonly ILogger<ProductCreateCommandHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationDbContext _dbContext;

        public ProductCreateCommandHandler(
            ILogger<ProductCreateCommandHandler> logger,
            ICurrentUserService currentUserService,
            IApplicationDbContext dbContext)
        {
            _logger = logger;
            _currentUserService = currentUserService;
            _dbContext = dbContext;
        }

        public async Task<string> Handle(ProductCreateCommand request, CancellationToken cancellationToken)
        {
            try
            {
                Store store = await _dbContext.Stores.SingleOrDefaultAsync(s =>
                    s.Uid == request.StoreUid &&
                    s.User.Id == _currentUserService.GetUserId() &&
                    s.IsActive == true, cancellationToken);
                if (store == null)
                {
                    throw new BadRequestException("Store doesn't exist.");
                }

                var product = new Product
                {
                    ArticleCode = ShortId.Generate(new GenerationOptions(true, false)),
                    Description = request.Description,
                    Name = request.Name,
                    Price = request.Price,
                    Quantity = request.Quantity,
                    Store = store,
                    ProductAttributes = request.ProductVariations.Any()
                        ? request.ProductVariations.Select(pv => new ProductAttribute()
                        {
                            Key = pv.Key,
                            ProductAttributeValues = String.IsNullOrEmpty(pv.Values)
                                ? null
                                : pv.Values.Split('|').Select(v => new ProductAttributeValue() { Value = v }).ToList()
                        }).ToList()
                        : null
                };
                _dbContext.Products.Add(product);


                if (request.CategoryUids.Any())
                {
                    var categories = await _dbContext.Categories.Where(c => c.IsActive && request.CategoryUids.Contains(c.Uid)).ToListAsync(cancellationToken);
                    if (!categories.Any())
                    {
                        throw new BadRequestException("Categories not found");
                    }

                    foreach (var category in categories)
                    {
                        product.ProductCategory.Add(new ProductCategory
                        {
                            Category = category,
                            ProductId = product.Id
                        });
                    }
                }

                if (request.MoreInfos.Any())
                {
                    var newMoreInfos = new List<ProductMoreInfo>();
                    foreach (var moreInfoRequest in request.MoreInfos)
                    {
                        newMoreInfos.Add(new ProductMoreInfo()
                        {
                            Product = product,
                            Title = moreInfoRequest.Title,
                            Info = moreInfoRequest.Info
                        });
                    }

                    if (newMoreInfos.Any())
                    {
                        _dbContext.ProductMoreInfos.AddRange(newMoreInfos);
                    }
                }

                if (request.ProductPairArticleCodes.Any())
                {
                    var newPairs = new List<ProductPair>();
                    foreach (var pairArticleCode in request.ProductPairArticleCodes)
                    {
                        var pairByArticleCode =
                            await _dbContext.Products.SingleOrDefaultAsync(p => p.ArticleCode == pairArticleCode, cancellationToken);
                        if (pairByArticleCode != null)
                        {
                            newPairs.Add(new ProductPair()
                            {
                                Product = product,
                                Pair = pairByArticleCode
                            });
                        }
                    }

                    if (newPairs.Any())
                    {
                        _dbContext.ProductPairs.AddRange(newPairs);
                    }
                }

                if (request.ProductSimilarArticleCodes.Any())
                {
                    var newSimilars = new List<ProductSimilar>();
                    foreach (var productSimilarArticleCode in request.ProductSimilarArticleCodes)
                    {
                        var productSimilar = await _dbContext.Products.SingleOrDefaultAsync(p =>
                            p.ArticleCode == productSimilarArticleCode && p.Store == store, cancellationToken);
                        if (productSimilar != null)
                        {
                            newSimilars.Add(new ProductSimilar
                            {
                                Product = product,
                                Similar = productSimilar
                            });
                        }
                    }

                    if (newSimilars.Any())
                    {
                        _dbContext.ProductSimilars.AddRange(newSimilars);
                    }
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                return product.Uid;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}