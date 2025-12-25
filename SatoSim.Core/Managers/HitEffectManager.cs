using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using static SatoSim.Core.Utils.TimingUtils;
using static SatoSim.Core.Utils.PlayfieldUtils;

namespace SatoSim.Core.Managers
{
    public class HitEffectManager
    {
        protected static Texture2D HanteiTextTexture;
        protected static Vector2 HanteiTextOrigin;
        
        protected static Texture2D BaseTexture;
        protected static Texture2D SlashTexture;
        protected static Texture2D RippleFlashTexture;
        protected static SpriteBatch SpriteBatch;

        protected static bool _extraEffects;
        
        private List<HitEffect> _activeEffects;
        
        public HitEffectManager(SpriteBatch spriteBatch, bool extraEffects, Texture2D hanteiTextTex, Texture2D baseTex, Texture2D slashTex, Texture2D rippleFlashFlashTex)
        {
            SpriteBatch = spriteBatch;
            
            BaseTexture = baseTex;
            SlashTexture = slashTex;
            RippleFlashTexture = rippleFlashFlashTex;
            _extraEffects = extraEffects;

            HanteiTextTexture = hanteiTextTex;
            HanteiTextOrigin = new Vector2(hanteiTextTex.Width, hanteiTextTex.Height / 7f) / 2;
            
            _activeEffects = new List<HitEffect>();
        }

        public void TriggerEffect(int locationId, int judgeId, int noteType, float rotation)
        {
            switch (noteType)
            {
                case 0:
                    _activeEffects.Add(new TapHitEffect(GetPlayfieldAnchorPosition(locationId), judgeId));
                    break;
                case 1:
                    _activeEffects.Add(new SlashHitEffect(GetPlayfieldAnchorPosition(locationId), judgeId, rotation));
                    break;
                case 2:
                    _activeEffects.Add(new RippleHitEffect(GetPlayfieldAnchorPosition(locationId), judgeId));
                    break;
            }
            
            _activeEffects.Add(new HanteiTextEffect(GetPlayfieldAnchorPosition(locationId), judgeId));
        }

        public void DrawEffects(GameTime gameTime)
        {
            List<int> doneIDs = new List<int>();
            
            for (var i = 0; i < _activeEffects.Count; i++)
            {
                _activeEffects[i].Draw(gameTime);
                
                if (_activeEffects[i].IsCompleted) doneIDs.Add(i);
            }
            
            for (var i = doneIDs.Count - 1; i > 0; i--)
            {
                _activeEffects.RemoveAt(doneIDs[i]);
            }
        }
        
        
        
        public abstract class HitEffect
        {
            protected Vector2 _position;
            protected int _judgmentId;
            protected float _progress;
            public bool IsCompleted => _progress >= 1f;
            protected Color _color;

            public HitEffect(Vector2 position, int judgeId)
            {
                _position = position;
                _judgmentId = judgeId;
            }

            public abstract void Draw(GameTime gameTime);
        }

        public class HanteiTextEffect : HitEffect
        {
            private Rectangle _srcRect;
            
            public HanteiTextEffect(Vector2 position, int judgeId) : base(position, judgeId)
            {
                _color = Color.White;
                _srcRect = new Rectangle(0, (int)(HanteiTextOrigin.Y * 2) * judgeId, (int)(HanteiTextOrigin.X * 2),
                    (int)(HanteiTextOrigin.Y * 2));
            }

            public override void Draw(GameTime gameTime)
            {
                float fadeOut = 1f - float.Clamp(_progress * 2f - 0.5f, 0, 1f);
                Vector2 posOffset = -Vector2.UnitY * ((float.Clamp(_progress * 4f, 0, 1f)) * 48f);

                SpriteBatch.Draw(HanteiTextTexture, _position + posOffset, _srcRect,
                    Color.White * fadeOut, 0f, HanteiTextOrigin,
                    Vector2.One * 0.75f, SpriteEffects.None, 1f);

                _progress += gameTime.GetElapsedSeconds();
            }
        }

        public class TapHitEffect : HitEffect
        {
            public TapHitEffect(Vector2 position, int judgeId) : base(position, judgeId)
            {
                _color = _judgmentId switch
                {
                    JUDGE_ID_FANTASTIC => Color.Magenta,
                    JUDGE_ID_E_GREAT or JUDGE_ID_L_GREAT => Color.LimeGreen,
                    JUDGE_ID_E_FINE or JUDGE_ID_L_FINE => Color.Yellow,
                    JUDGE_ID_E_MISS or JUDGE_ID_L_MISS => Color.Transparent,
                    _ => Color.White
                };
            }

            public override void Draw(GameTime gameTime)
            {
                SpriteBatch.Draw(BaseTexture, _position, null, _color * (0.5f - _progress), 0f, Vector2.One * 256f,
                    Vector2.One * (0.25f + _progress * 0.25f),
                    SpriteEffects.None, 1f);
                SpriteBatch.Draw(BaseTexture, _position, null, _color * (0.5f - _progress), 0f, Vector2.One * 256f,
                    Vector2.One * (0.25f - _progress * 0.25f),
                    SpriteEffects.None, 1f);
                
                _progress += gameTime.GetElapsedSeconds() * 3f;
            }
        }

        public class SlashHitEffect : HitEffect
        {
            private float _rotation;
            
            public SlashHitEffect(Vector2 position, int judgeId, float slashDirection) : base(position, judgeId)
            {
                _color = _judgmentId switch
                {
                    JUDGE_ID_FANTASTIC => Color.Magenta,
                    JUDGE_ID_E_GREAT or JUDGE_ID_L_GREAT => Color.LimeGreen,
                    JUDGE_ID_E_FINE or JUDGE_ID_L_FINE => Color.Yellow,
                    JUDGE_ID_E_MISS or JUDGE_ID_L_MISS => Color.Transparent,
                    _ => Color.White
                };

                _rotation = slashDirection;
            }

            public override void Draw(GameTime gameTime)
            {
                SpriteBatch.Draw(BaseTexture, _position, null, _color * (0.5f - _progress), 0f, Vector2.One * 256f,
                    Vector2.One * (0.25f + _progress * 0.25f),
                    SpriteEffects.None, 1f);
                SpriteBatch.Draw(BaseTexture, _position, null, _color * (0.5f - _progress), 0f, Vector2.One * 256f,
                    Vector2.One * (0.25f - _progress * 0.25f),
                    SpriteEffects.None, 1f);

                if (_judgmentId is not (JUDGE_ID_L_MISS or JUDGE_ID_E_MISS))
                    SpriteBatch.Draw(SlashTexture, _position, null, Color.LightYellow * (0.5f - _progress * 3f),
                        _rotation,
                        Vector2.One * 256f,
                        Vector2.One * (1.3f - _progress * 0.25f),
                        SpriteEffects.None, 1f);
                
                _progress += gameTime.GetElapsedSeconds() * 3f;
            }
        }

        public class RippleHitEffect : HitEffect
        {
            public RippleHitEffect(Vector2 position, int judgeId) : base(position, judgeId)
            {
                _color = _judgmentId switch
                {
                    JUDGE_ID_FANTASTIC => Color.Magenta,
                    JUDGE_ID_E_GREAT or JUDGE_ID_L_GREAT => Color.LimeGreen,
                    JUDGE_ID_E_FINE or JUDGE_ID_L_FINE => Color.Yellow,
                    JUDGE_ID_E_MISS or JUDGE_ID_L_MISS => Color.Transparent,
                    _ => Color.White
                };
            }

            public override void Draw(GameTime gameTime)
            {
                SpriteBatch.Draw(BaseTexture, _position, null, _color * (1f - _progress * 3f), 0f, Vector2.One * 256f,
                    Vector2.One * (0.25f + _progress * 0.25f),
                    SpriteEffects.None, 1f);
                SpriteBatch.Draw(BaseTexture, _position, null, _color * (1f - _progress * 3f), 0f, Vector2.One * 256f,
                    Vector2.One * (0.25f - _progress * 0.25f),
                    SpriteEffects.None, 1f);

                if (_judgmentId == JUDGE_ID_FANTASTIC)
                {
                    SpriteBatch.Draw(BaseTexture, _position, null, Color.White * (1f - _progress * 3f), 0f,
                        Vector2.One * 256f,
                        Vector2.One * (0.5f - _progress),
                        SpriteEffects.None, 1f);
                    SpriteBatch.Draw(BaseTexture, _position, null, Color.White * (1f - _progress * 3f), 0f,
                        Vector2.One * 256f,
                        Vector2.One * (0.5f + _progress),
                        SpriteEffects.None, 1f);
                    
                    if (_extraEffects)
                    {
                        SpriteBatch.Draw(RippleFlashTexture, _position, null, Color.PaleGreen * (0.5f - _progress * 6f), 0f,
                            Vector2.One * 256f,
                            Vector2.One * (2.3f - _progress),
                            SpriteEffects.None, 1f);
                    }
                }

                _progress += gameTime.GetElapsedSeconds();
            }
        }
        
    }
}