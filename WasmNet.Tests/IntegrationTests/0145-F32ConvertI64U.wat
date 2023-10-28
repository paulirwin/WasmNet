;; invoke: convert
;; expect: (f32:42000000000)

(module
    (func (export "convert") (result f32)
        i64.const 42000000000
        f32.convert_i64_u
    )
)